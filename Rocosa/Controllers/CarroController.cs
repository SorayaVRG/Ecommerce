using Braintree;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Rocosa_AccesoDatos.Datos;
using Rocosa_AccesoDatos.Datos.Repositorio.IRepositorio;
using Rocosa_Modelos;
using Rocosa_Modelos.ViewModels;
using Rocosa_Utilidades;
using Rocosa_Utilidades.BrainTree;
using System.Security.Claims;
using System.Text;

namespace Rocosa.Controllers
{
    [Authorize]
    public class CarroController : Controller
    {
        
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailSender _emailSender;
        private readonly IProductoRepositorio _productoRepo;
        private readonly IUsuarioAplicacionRepositorio _usuarioRepo;
        private readonly IOrdenRepositorio _ordenRepo;
        private readonly IOrdenDetalleRepositorio _ordenDetalleRepo;
        private readonly IVentaRepositorio _ventaRepo;
        private readonly IVentaDetalleRepositorio _ventaDetalleRepo;
        private readonly IBrainTreeGate _brain;


        [BindProperty]
        public ProductoUsuarioVM productoUsuarioVM { get; set; }

        public CarroController(IWebHostEnvironment webHostEnvironment, IEmailSender emailSender,
                              IProductoRepositorio productoRepo,
                              IUsuarioAplicacionRepositorio usuarioRepo,
                              IOrdenRepositorio ordenRepo,
                              IOrdenDetalleRepositorio ordenDetalleRepo,
                              IVentaRepositorio ventaRepo,
                              IVentaDetalleRepositorio ventaDetalleRepo,
                              IBrainTreeGate brain)
        {
            _productoRepo = productoRepo;
            _usuarioRepo = usuarioRepo;
            _ordenRepo=ordenRepo;
            _ordenDetalleRepo = ordenDetalleRepo;
            _webHostEnvironment = webHostEnvironment;
            _emailSender = emailSender;
            _ventaRepo = ventaRepo;
            _ventaDetalleRepo = ventaDetalleRepo;
            _brain = brain;
        }


        
        public IActionResult Index()
        {
            List<CarroCompra> carroCompraList = new List<CarroCompra>();

            if (HttpContext.Session.Get<IEnumerable<CarroCompra>>(WC.SessionCarroCompras)!= null &&
                HttpContext.Session.Get<IEnumerable<CarroCompra>>(WC.SessionCarroCompras).Count()>0)
            {
                carroCompraList = HttpContext.Session.Get<List<CarroCompra>>(WC.SessionCarroCompras);
            }

            List<int> prodEnCarro = carroCompraList.Select(i=>i.ProductoId).ToList();
            //IEnumerable<Producto> prodList = _db.Producto.Where(p => prodEnCarro.Contains(p.Id));
            IEnumerable<Producto> prodList = _productoRepo.ObtenerTodos(p => prodEnCarro.Contains(p.Id));
            List<Producto> prodListFinal = new List<Producto>();

            foreach(var objCarro in carroCompraList)
            {
                Producto prodtemp = prodList.FirstOrDefault(p => p.Id == objCarro.ProductoId);
                prodtemp.TempMetroCuadrado = objCarro.MetroCuadrado;
                prodListFinal.Add(prodtemp);
            }

            return View(prodListFinal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Index")]
        public IActionResult IndexPost(IEnumerable<Producto> ProdLista)
        {
            List<CarroCompra> carroCompraLista = new List<CarroCompra>();

            foreach (Producto prod in ProdLista)
            {
                carroCompraLista.Add(new CarroCompra
                {
                    ProductoId = prod.Id,
                    MetroCuadrado = prod.TempMetroCuadrado
                });
            }
            HttpContext.Session.Set(WC.SessionCarroCompras, carroCompraLista);

            return RedirectToAction(nameof(Resumen));
        }

        public IActionResult Resumen()
        {

            UsuarioAplicacion usuarioAplicacion;

            if(User.IsInRole(WC.AdminRole))
            {
                if(HttpContext.Session.Get<int>(WC.SessionOrdenId) !=0)
                {
                    Orden orden = _ordenRepo.ObtenerPrimero(u => u.Id ==
                                        HttpContext.Session.Get<int>(WC.SessionOrdenId));
                    usuarioAplicacion = new UsuarioAplicacion()
                    {
                        Email = orden.Email,
                        NombreCompleto = orden.NombreCompleto,
                        PhoneNumber = orden.Telefono
                    };
                }
                else  // Si no pertenece a una Orden
                {
                    usuarioAplicacion = new UsuarioAplicacion();
                }
                var gateway = _brain.GetGateway();
                var clientToken = gateway.ClientToken.Generate();
                ViewBag.ClientToken = clientToken;


            }
            else
            {
                // Traer el Usuario Conectado
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                //
                usuarioAplicacion = _usuarioRepo.ObtenerPrimero(u => u.Id == claim.Value);

            }

            List<CarroCompra> carroCompraList = new List<CarroCompra>();

            if (HttpContext.Session.Get<IEnumerable<CarroCompra>>(WC.SessionCarroCompras) != null &&
                HttpContext.Session.Get<IEnumerable<CarroCompra>>(WC.SessionCarroCompras).Count() > 0)
            {
                carroCompraList = HttpContext.Session.Get<List<CarroCompra>>(WC.SessionCarroCompras);
            }

            List<int> prodEnCarro = carroCompraList.Select(i => i.ProductoId).ToList();
            //IEnumerable<Producto> prodList = _db.Producto.Where(p => prodEnCarro.Contains(p.Id));
            IEnumerable<Producto> prodList = _productoRepo.ObtenerTodos(p => prodEnCarro.Contains(p.Id));

           productoUsuarioVM = new ProductoUsuarioVM()
            {
               //UsuarioAplicacion = _db.UsuarioAplicacion.FirstOrDefault(u => u.Id == claim.Value),
               UsuarioAplicacion = usuarioAplicacion,              
            };

            foreach(var carro in carroCompraList)
            {
                Producto prodTemp = _productoRepo.ObtenerPrimero(p => p.Id == carro.ProductoId);
                prodTemp.TempMetroCuadrado = carro.MetroCuadrado;
                productoUsuarioVM.ProductoLista.Add(prodTemp);
            }

            return View(productoUsuarioVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Resumen")]
        public async Task<IActionResult> ResumenPost(IFormCollection collection,ProductoUsuarioVM productoUsuarioVM)
        {
            var claimsidentity = (ClaimsIdentity)User.Identity;
            var claim = claimsidentity.FindFirst(ClaimTypes.NameIdentifier);

            if(User.IsInRole(WC.AdminRole))
            {
                // Creamos la Venta
                Venta venta = new Venta()
                {
                    CreadoPorUsusarioId = claim.Value,
                    FinalVentaTotal = productoUsuarioVM.ProductoLista.Sum(x=>x.TempMetroCuadrado*x.Precio),
                    Direccion = productoUsuarioVM.UsuarioAplicacion.Direccion,
                    Ciudad = productoUsuarioVM.UsuarioAplicacion.Ciudad,
                    Telefono = productoUsuarioVM.UsuarioAplicacion.PhoneNumber,
                    NombreCompleto = productoUsuarioVM.UsuarioAplicacion.NombreCompleto,
                    Email = productoUsuarioVM.UsuarioAplicacion.Email,
                    FechaVenta = DateTime.Now,
                    EstadoVenta = WC.EstadoPendiente
                };

                _ventaRepo.Agregar(venta);
                _ventaRepo.Grabar();

                foreach(var prod in productoUsuarioVM.ProductoLista)
                {
                    VentaDetalle ventaDetalle = new VentaDetalle()
                    {
                        VentaId = venta.Id,
                        PrecioPorMetroCuadrado = prod.Precio,
                        MetroCuadrado = prod.TempMetroCuadrado,
                        ProductoId = prod.Id
                    };
                    _ventaDetalleRepo.Agregar(ventaDetalle);
                }
                _ventaDetalleRepo.Grabar();

                string nonceFromTheClient = collection["payment_method_nonce"];

                var request = new TransactionRequest
                {
                    Amount = Convert.ToDecimal(venta.FinalVentaTotal),
                    PaymentMethodNonce = nonceFromTheClient,
                    OrderId = venta.Id.ToString(),
                    Options = new TransactionOptionsRequest
                    {
                        SubmitForSettlement = true
                    }
                };

                var gateway = _brain.GetGateway();
                Result<Transaction> result = gateway.Transaction.Sale(request);

                // Modificar la Venta
                if(result.Target.ProcessorResponseText=="Approved")
                {
                    venta.TransaccionId = result.Target.Id;
                    venta.EstadoVenta = WC.EstadoAprobado;
                }
                else
                {
                    venta.EstadoVenta = WC.EstadoCancelado;
                }
                _ventaRepo.Grabar();

                return RedirectToAction(nameof(Confirmacion), new {id= venta.Id});

            }
            else
            {
                // creamos la Orden
                var rutaTemplate = _webHostEnvironment.WebRootPath + Path.DirectorySeparatorChar.ToString()
                                + "templates" + Path.DirectorySeparatorChar.ToString()
                                + "PlantillaOrden.html";

                var subject = "Nueva Orden";
                string HtmlBody = "";



                using (StreamReader sr = System.IO.File.OpenText(rutaTemplate))
                {
                    HtmlBody = sr.ReadToEnd();
                }

                //Nombre: { 0}
                //Email: { 1}
                //Telefono: { 2}
                // Productos: { 3}

                StringBuilder productoListaSB = new StringBuilder();

                foreach (var prod in productoUsuarioVM.ProductoLista)
                {
                    productoListaSB.Append($" - Nombre: { prod.NombreProducto } <span style='font-size:14px;'> (ID: { prod.Id })</span><br />");
                }

                string messageBody = string.Format(HtmlBody,
                                        productoUsuarioVM.UsuarioAplicacion.NombreCompleto,
                                        productoUsuarioVM.UsuarioAplicacion.Email,
                                        productoUsuarioVM.UsuarioAplicacion.PhoneNumber,
                                        productoListaSB.ToString());

                await _emailSender.SendEmailAsync(WC.EmailAdmin, subject, messageBody);

                // Grabar la Orden y Detalle en la BD

                Orden orden = new Orden()
                {
                    UsuarioAplicacionId = claim.Value,
                    NombreCompleto = productoUsuarioVM.UsuarioAplicacion.NombreCompleto,
                    Email = productoUsuarioVM.UsuarioAplicacion.Email,
                    Telefono = productoUsuarioVM.UsuarioAplicacion.PhoneNumber,
                    FechaOrden = DateTime.Now
                };

                _ordenRepo.Agregar(orden);
                _ordenRepo.Grabar();

                foreach (var prod in productoUsuarioVM.ProductoLista)
                {
                    OrdenDetalle ordenDetalle = new OrdenDetalle()
                    {
                        OrdenId = orden.Id,
                        ProductoId = prod.Id
                    };
                    _ordenDetalleRepo.Agregar(ordenDetalle);

                }
                _ordenDetalleRepo.Grabar();
            }

            


            return RedirectToAction(nameof(Confirmacion));
        }

        public IActionResult Confirmacion(int id=0)
        {
            Venta venta = _ventaRepo.ObtenerPrimero(v => v.Id == id);

            HttpContext.Session.Clear();
            return View(venta);
        }

        public IActionResult Remover(int Id)
        {
            List<CarroCompra> carroCompraList = new List<CarroCompra>();

            if (HttpContext.Session.Get<IEnumerable<CarroCompra>>(WC.SessionCarroCompras) != null &&
                HttpContext.Session.Get<IEnumerable<CarroCompra>>(WC.SessionCarroCompras).Count() > 0)
            {
                carroCompraList = HttpContext.Session.Get<List<CarroCompra>>(WC.SessionCarroCompras);
            }

            carroCompraList.Remove(carroCompraList.FirstOrDefault(p => p.ProductoId == Id));
            HttpContext.Session.Set(WC.SessionCarroCompras, carroCompraList);

            return RedirectToAction(nameof(Index));

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarCarro(IEnumerable<Producto> ProdLista)
        {
            List<CarroCompra> carroCompraLista = new List<CarroCompra>();

            foreach(Producto prod in ProdLista)
            {
                carroCompraLista.Add(new CarroCompra
                {
                   ProductoId = prod.Id,
                   MetroCuadrado =prod.TempMetroCuadrado
                });
            }
            HttpContext.Session.Set(WC.SessionCarroCompras, carroCompraLista);

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Limpiar()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

    }
}
