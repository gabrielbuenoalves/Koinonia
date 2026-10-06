using Microsoft.EntityFrameworkCore;

namespace Koinonia.Api.Dados;

public static class SemeadorDeRoles
{
    public static void Semear(KoinoniaDbContext contexto)
    {
        Garantir("Funcionário", ["visao-geral", "ponto", "registrar-propria-batida"]);
        Garantir("Gestão", CodigosDePermissao.Todas);
        contexto.SaveChanges();

        void Garantir(string nome, IEnumerable<string> codigos)
        {
            var role = contexto.Roles.Include(item => item.Permissoes).SingleOrDefault(item => item.Nome == nome);
            if (role is null)
            {
                role = new Role { Id = Guid.NewGuid(), Nome = nome };
                contexto.Roles.Add(role);
            }

            var presentes = role.Permissoes.Select(item => item.Codigo).ToHashSet();
            foreach (var codigo in codigos.Where(codigo => !presentes.Contains(codigo)))
                role.Permissoes.Add(new PermissaoDaRole { Codigo = codigo });
        }
    }
}
