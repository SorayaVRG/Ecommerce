﻿using System.ComponentModel.DataAnnotations;

namespace Rocosa_Modelos
{
    public class Categoria
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage ="Nombre de Categoria es Obligatorio.")]
        public string NombreCategoria { get; set; }

        [Required(ErrorMessage ="Orden es Obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage ="El Orden debe de ser Mayor a cero.")]
        public int MostrarOrden { get; set; }
    }
}
