using Microsoft.EntityFrameworkCore;
using Rocosa_AccesoDatos.Datos.Repositorio.IRepositorio;
using Rocosa_Modelos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rocosa_AccesoDatos.Datos.Repositorio
{
    public class CategoriaRepositorio : Repositorio<Categoria>, ICategoriaRepositorio
    {

        private readonly ApplicationDbContext _db;

        public CategoriaRepositorio(ApplicationDbContext db): base(db)
        {
            _db = db;
        }
        public void Actualizar(Categoria categoria)
        {
            var catAnterior = _db.Categoria.FirstOrDefault(c=>c.Id == categoria.Id);
            if(catAnterior != null)
            {
                catAnterior.NombreCategoria = categoria.NombreCategoria;
                catAnterior.MostrarOrden = categoria.MostrarOrden;
            }
        }

        public bool ExisteNumeroOrden(int numeroOrden)
        {
            // Verificar si ya existe una categoría con el mismo número de orden
            return _db.Categoria.Any(c => c.MostrarOrden == numeroOrden);
        }

        public Categoria ObtenerPorId(int id)
        {
            return _db.Categoria.Find(id);
        }
    }
}
