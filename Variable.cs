using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emulador
{
    public class Variable
    {
        public enum TipoDato
        {
            Char, Int, Float
        }
        private TipoDato tipo;
        private string nombre;
        private float valor;
        public Variable(TipoDato tipo, string nombre, float valor = 0)
        {
            this.tipo = tipo;
            this.nombre = nombre;
            this.valor = valor;
        }

        //SECTION - Se modifico los parametros de SetValor para dar linea de error
        public void setValor(float valor, int linea, int columna, StreamWriter log, TipoDato MaxTipo)
        {
            if (MaxTipo <= tipo)
            {
                this.valor = valor;
            }
            else
            {
                throw new Error("Semántico: no se puede asignar un " + MaxTipo + " a un " + tipo, log, linea, columna);
            }
        }
        //!SECTION

        public float Valor
        {
            get => valor;
        }
        public string Nombre
        {
            get => nombre;
            set => nombre = value;
        }
        public TipoDato Tipo
        {
            get => tipo;
        }
        public static TipoDato valorTipoDato(float valor, TipoDato maxTipo,bool Casteo = false)
        {
            //REVIEW - Cambie la logica de esta comparativa para agregar el caso de casteo
            if (valor % 1 != 0)  
            {
                return TipoDato.Float;
            }
            
            if(Casteo)
            {
                if (valor >= 0 && valor <= 255)
                {
                    return TipoDato.Char;
                }
                return TipoDato.Int;
            }

            //Si el entero proviene de un float, obliga a que sea float
            if (maxTipo == TipoDato.Float)  
            {
                return TipoDato.Float;
            }

            if (valor >= 0 && valor <= 9)
            {
                return TipoDato.Char;
            }

            return TipoDato.Int;
       }
    }

}