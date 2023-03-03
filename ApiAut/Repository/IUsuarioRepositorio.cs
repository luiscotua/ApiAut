using ApiAut.Models.Dto;

namespace ApiAut.Repository
{
    public interface IUsuarioRepositorio
    {
        Task<string> Login(UsuarioDto usuario);
        Task<TokenAzureDto> RegistroUsuarioAzure(UsuarioDto usuario);
    }
}
