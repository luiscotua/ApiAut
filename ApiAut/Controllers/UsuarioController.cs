using ApiAut.Models.Dto;
using ApiAut.Repository;
using Microsoft.AspNetCore.Mvc;

namespace ApiAut.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioRepositorio _usuarioRepositorio;
        public UsuarioController(IUsuarioRepositorio usuarioRepositorio)
        {
            _usuarioRepositorio = usuarioRepositorio;
        }


        [HttpPost("LoginAzure")]
        public async Task<string> LoginAzure(UsuarioDto usuario)
        {
            var respuestaToken = await _usuarioRepositorio.Login(usuario);

            return respuestaToken;
        }
    }
}
