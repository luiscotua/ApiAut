using ApiAut.Data;
using ApiAut.Models;
using ApiAut.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ApiAut.Repository
{
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;
        public UsuarioRepositorio(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public async Task<string> Login(UsuarioDto usuario)
        {
            //se obtiene token de registro azure 
            var resultTokenAzure = await RegistroUsuarioAzure(usuario);

            //se obtienen las claims del token azure
            var claimsTokenAzure = await GetTokenInfo(resultTokenAzure.access_token);

            var user = await _db.Usuarios.FirstOrDefaultAsync(x => x.UserName.ToLower().Equals(usuario.UserName.ToLower()));
            if (user == null)
            {
                return "Nouser";
            }
            //else if (!ValidarPasswordHash(usuario.Password, user.PasswordHash, user.PasswordSalt))
            //{
            //    return "Nopass";
            //}
            else
            {   
                //Se crea nuevo token 
                return CrearNuevoToken(user, claimsTokenAzure);
            }
        }

        public async Task<TokenAzureDto> RegistroUsuarioAzure(UsuarioDto usuario)
        {
            var client = new HttpClient();
            var uri = "https://login.microsoftonline.com/newsoftsso.onmicrosoft.com/oauth2/token?api-version=1.0";
            var pairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("resource", "https://graph.microsoft.com"),
                new KeyValuePair<string, string>("client_id", "f55dae5a-b9b9-45c6-aa9c-f417f5fecd8b"),
                new KeyValuePair<string, string>("client_secret", "L6Z8Q~TZg6dwdyUDFcdLtse45P-jWJ53js054bcP"),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>($"username", usuario.UserName),
                new KeyValuePair<string, string>("password", usuario.Password),
                new KeyValuePair<string, string>("scope", "openid")
            };

            var content = new FormUrlEncodedContent(pairs);

            var response = client.PostAsync(uri, content).Result;

            TokenAzureDto resultado = new TokenAzureDto();

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();

                resultado = JsonConvert.DeserializeObject<TokenAzureDto>(jsonResponse);

                //registro usuario en db si no existe
                if (!await UserExiste(usuario.UserName))
                {
                    await Registrar(new Models.Usuario { UserName = usuario.UserName }, usuario.Password);
                }

                return resultado;
            }

            return resultado;
        }

        public async Task<string> Registrar(Usuario usuario, string password)
        {
            try
            {
                //if (await UserExiste(usuario.UserName))
                //{
                //    return -1;
                //}

                CrearPasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

                usuario.PasswordHash = passwordHash;
                usuario.PasswordSalt = passwordSalt;

                await _db.AddAsync(usuario);
                await _db.SaveChangesAsync();
                return usuario.Id.ToString();
            }
            catch (Exception ex)
            {
                return "-500";
            }
        }

        public async Task<bool> UserExiste(string userName)
        {
            if (await _db.Usuarios.AnyAsync(x => x.UserName.ToLower().Equals(userName.ToLower())))
            {
                return true;
            }
            return false;
        }

        private void CrearPasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private string CrearNuevoToken(Usuario usuario, Dictionary<string, string> claimsTokenAzure)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.UserName),
                new Claim(ClaimTypes.Email, usuario.UserName),
                new Claim("app_displayname", claimsTokenAzure["app_displayname"]),
                new Claim("family_name", claimsTokenAzure["family_name"]),
                new Claim("given_name", claimsTokenAzure["given_name"]),
                new Claim("name", claimsTokenAzure["name"]),
                new Claim("scp", claimsTokenAzure["scp"]),
                new Claim("tid", claimsTokenAzure["tid"]),
                new Claim("unique_name", claimsTokenAzure["unique_name"]),
                new Claim("upn", claimsTokenAzure["upn"])

            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = System.DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        private async Task<Dictionary<string, string>> GetTokenInfo(string token)
        {
            var TokenInfo = new Dictionary<string, string>();

            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(token);
            var claims = jwtSecurityToken.Claims.ToList();

            foreach (var claim in claims)
            {
                TokenInfo.Add(claim.Type, claim.Value);
            }

            return TokenInfo;
        }
    }
}
