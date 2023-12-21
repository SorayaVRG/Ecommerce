
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rocosa_AccesoDatos.Datos;
using Rocosa_AccesoDatos.Datos.Repositorio.IRepositorio;
using Rocosa_Modelos;
using Rocosa_Utilidades;
using Syncfusion.EJ2.Linq;

namespace Rocosa.Controllers
{
    [Authorize(Roles = WC.AdminRole)]
    public class CategoriaController : Controller
    {
        private readonly ICategoriaRepositorio _catRepo;

        public CategoriaController(ICategoriaRepositorio catRepo)
        {
            _catRepo = catRepo;
        }

        public IActionResult Index()
        {
            IEnumerable<Categoria> lista = _catRepo.ObtenerTodos().OrderBy(c=>c.MostrarOrden);

            return View(lista);
        }

        // Get
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Crear(Categoria categoria)
        {
            if(_catRepo.ExisteNumeroOrden(categoria.MostrarOrden))
            {
                ModelState.AddModelError("MostrarOrden", "Ya existe una categoría con este número de orden.");
            }
            if (ModelState.IsValid)
            {
                _catRepo.Agregar(categoria);
                _catRepo.Grabar();
                TempData[WC.Exitosa] = "Categoria creada Exitosamente!";
                return RedirectToAction(nameof(Index));
            }
            TempData[WC.Error] = "Error al Crear nueva Categoria";
            return View(categoria);

            
        }

        // Get Editar
        public IActionResult Editar(int? Id)
        {
            if (Id==null || Id==0)
            {
                return NotFound();
            }
            var obj = _catRepo.Obtener(Id.GetValueOrDefault());
            if(obj == null)
            {
                return NotFound();
            }

            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(Categoria categoria)
        {
            var categoriaExistente = _catRepo.ObtenerPorId(categoria.Id);
            if (categoriaExistente != null)
            {
                if(categoriaExistente.MostrarOrden != categoria.MostrarOrden)
                {
                    if (_catRepo.ExisteNumeroOrden(categoria.MostrarOrden))
                    {
                        ModelState.AddModelError("MostrarOrden", "Ya existe una categoría con este número de orden.");
                    }
                }
            }
            if (ModelState.IsValid)
            {
                _catRepo.Actualizar(categoria);
                _catRepo.Grabar();
                TempData[WC.Exitosa] = "Categoria actualizada Exitosamente!";
                return RedirectToAction(nameof(Index));
            }
            TempData[WC.Error] = "Error al Actualizar nueva Categoria";
            return View(categoria);
        }

        // Get Eliminar
        public IActionResult Eliminar(int? Id)
        {
            if (Id == null || Id == 0)
            {
                return NotFound();
            }
            var obj = _catRepo.Obtener(Id.GetValueOrDefault());
            if (obj == null)
            {
                return NotFound();
            }

            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(Categoria categoria)
        {
            if (categoria == null)
            {
                return NotFound();
            }
            _catRepo.Remover(categoria);
            _catRepo.Grabar();
            TempData[WC.Exitosa] = "Categoria Eliminada Exitosamente!";
            return RedirectToAction(nameof(Index));            

        }

    }
}
