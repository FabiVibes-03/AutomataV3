using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Data.Common;
using System.IO.Compression;
using Microsoft.VisualBasic;

namespace Emulador
{
    public class Lexico : Token, IDisposable
    {
        public StreamReader archivo; //Archivo de entrada
        public StreamWriter log; //Archivo de salida
        public StreamWriter error = new StreamWriter("Error.log"); //Archivo de salida
        DateTime ahora = DateTime.Now; //Fecha y hora
        public StreamWriter asm; //Archivo de salida
        public int linea = 1; //Número de línea
        const int F = -1; //Estado de aceptación final
        const int E = -2; //Estado de error
        public int columna = 1; //Número de columna
        protected int characterCounter; //NOTE - Agregarlo al constructor
        readonly int[,] TRAND = {
            {  0,  1,  2, 33,  1, 12, 14,  8,  9, 10, 11, 23, 16, 16, 18, 20, 21, 26, 25, 27, 29, 32, 34,  0,  F, 33  },
            {  F,  1,  1,  F,  1,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  2,  3,  5,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  E,  E,  4,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E  },
            {  F,  F,  4,  F,  5,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  E,  E,  7,  E,  E,  6,  6,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E  },
            {  E,  E,  7,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E  },
            {  F,  F,  7,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F, 13,  F,  F,  F,  F,  F, 13,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F, 13,  F,  F,  F,  F, 13,  F,  F,  F,  F,  F,  F, 15,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F, 17,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F, 19,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F, 19,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F, 22,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F, 24,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F, 24,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F, 24,  F,  F,  F,  F,  F,  F, 24,  F,  F,  F,  F,  F,  F,  F  },
            { 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 28, 27, 27, 27, 27,  E, 27  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            { 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30  },
            {  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E,  E, 31,  E,  E,  E,  E,  E  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F, 32,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F  },
            {  F,  F,  F,  F,  F,  F,  F,  F,  F,  F,  F, 17, 36,  F,  F,  F,  F,  F,  F,  F,  F,  F, 35,  F,  F,  F  },
            { 35, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35, 35,  0, 35, 35  },
            { 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 37, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36,  E, 36  },
            { 36, 36, 36, 36, 36, 36, 35, 36, 36, 36, 36, 36, 37, 36, 36, 36, 36, 36, 36, 36, 36, 36,  0, 36,  E, 36  }
        };

        public Lexico(string nombreArchivo = "prueba.cpp")
        {

            string nombreArchivoWithoutExt = Path.GetFileNameWithoutExtension(nombreArchivo);   /* Obtenemos el nombre del archivo sin la extensión para poder crear el .log y .asm */
            error.AutoFlush = true;
            characterCounter = 0;

            if (File.Exists(nombreArchivo))
            {
                if (Path.GetExtension(nombreArchivo) != ".cpp")
                {
                    throw new Error("El archivo debe ser de extensión .cpp", error);
                }
                log = new StreamWriter(nombreArchivoWithoutExt + ".log");
                asm = new StreamWriter(nombreArchivoWithoutExt + ".asm");
                log.AutoFlush = true;
                asm.AutoFlush = true;
                archivo = new StreamReader(nombreArchivo);
                //NOTE - Cambie el log a asm
                asm.WriteLine(";Archivo: " + nombreArchivo);
                asm.WriteLine(";Fecha y hora: " + ahora.ToString());
                asm.WriteLine(";----------------------------------");

                asm.WriteLine("%include \"io.inc\"");
                asm.WriteLine("segment .text");
                asm.WriteLine("global main");
                asm.WriteLine("main:");

            }
            else
            {
                throw new Error(";El archivo " + nombreArchivo + " no existe", error);    /* Defino una excepción que indica que existe un error con el archivo en caso de no ser encontrado */
            }
        }

        public void Dispose()
        {
            archivo.Close();
            log.Close();
            asm.Close();
        }

        private int Columna(char c)
        {
            return c switch
            {
                _ when c == '\n' => 23,
                'e' or 'E' => 4,
                '.' => 3,
                '+' => 5,
                '-' => 6,
                ';' => 7,
                '{' => 8,
                '}' => 9,
                '?' => 10,
                '=' => 11,
                '*' => 12,
                '%' => 13,
                '&' => 14,
                '|' => 15,
                '!' => 16,
                '<' => 17,
                '>' => 18,
                '"' => 19,
                '\'' => 20,
                '#' => 21,
                '/' => 22,
                _ when finArchivo() => 24,
                _ when char.IsWhiteSpace(c) => 0,
                _ when char.IsLetter(c) => 1,
                _ when char.IsDigit(c) => 2,
                _ => 25

            };
        }
        private void Clasifica(int estado)
        {
            switch (estado)
            {
                case 1: Clasificacion = Tipos.Identificador; break;
                case 2: Clasificacion = Tipos.Numero; break;
                case 8: Clasificacion = Tipos.FinSentencia; break;
                case 9: Clasificacion = Tipos.InicioBloque; break;
                case 10: Clasificacion = Tipos.FinBloque; break;
                case 11: Clasificacion = Tipos.OperadorTernario; break;
                case 12: Clasificacion = Tipos.OperadorTermino; break;
                case 13: Clasificacion = Tipos.OperadorTermino; break;
                case 14: Clasificacion = Tipos.OperadorTermino; break;
                case 15: Clasificacion = Tipos.Puntero; break;
                case 16: Clasificacion = Tipos.OperadorFactor; break;
                case 17: Clasificacion = Tipos.IncrementoFactor; break;
                case 18: Clasificacion = Tipos.Caracter; break;
                case 19: Clasificacion = Tipos.OperadorLogico; break;
                case 20: Clasificacion = Tipos.Caracter; break;
                case 21: Clasificacion = Tipos.OperadorLogico; break;
                case 22: Clasificacion = Tipos.OperadorRelacional; break;
                case 23: Clasificacion = Tipos.Asignacion; break;
                case 24: Clasificacion = Tipos.OperadorRelacional; break;
                case 25: Clasificacion = Tipos.OperadorRelacional; break;
                case 26: Clasificacion = Tipos.OperadorRelacional; break;
                case 27: Clasificacion = Tipos.Cadena; break;
                case 29: Clasificacion = Tipos.Caracter; break;
                case 32: Clasificacion = Tipos.Caracter; break;
                case 33: Clasificacion = Tipos.Caracter; break;
                case 34: Clasificacion = Tipos.OperadorFactor; break;
            }
        }
        public void nextToken()
        {
            char c;
            string buffer = "";
            int estado = 0;
            while (estado >= 0)
            {
                c = (char)archivo.Peek();
                estado = TRAND[estado, Columna(c)];
                Clasifica(estado);
                if (estado >= 0)
                {
                    archivo.Read();
                    if (c == '\n')
                    {
                        linea++;
                        columna = 1;
                    }
                    else
                    {
                        columna++;
                    }
                    if (estado > 0)
                    {
                        buffer += c;
                    }
                    else
                    {
                        buffer = "";
                    }
                }
            }
            if (estado == E)
            {
                if (Clasificacion == Tipos.Cadena)
                {
                    throw new Error("léxico, se esperaba un cierre de cadena", log, linea, columna);
                }
                else if (Clasificacion == Tipos.Caracter)
                {
                    throw new Error("léxico, se esperaba un cierre de comilla simple", log, linea, columna);
                }
                else if (Clasificacion == Tipos.Numero)
                {
                    throw new Error("léxico, se esperaba un dígito", log, linea, columna);
                }
                else
                {
                    throw new Error("léxico, se espera fin de comentario", log, linea, columna);
                }
            }
            Contenido = buffer;
            if (!finArchivo())
            {
                if (Clasificacion == Tipos.Identificador)
                {
                    switch (Contenido)
                    {
                        case "char":
                        case "int":
                        case "float":
                            Clasificacion = Tipos.TipoDato;
                            break;
                        case "if":
                        case "else":
                        case "do":
                        case "while":
                        case "for":
                            Clasificacion = Tipos.PalabraReservada;
                            break;
                        case "abs":
                        case "ceil":
                        case "pow":
                        case "sqrt":
                        case "exp":
                        case "floor":
                        case "log10":
                        case "log2":
                        case "rand":
                        case "trunc":
                        case "round":
                            Clasificacion = Tipos.FuncionMatematica;
                            break;
                    }
                }
                //log.WriteLine(Contenido + " = " + Clasificacion);
            }
        }
        public bool finArchivo()
        {
            return archivo.EndOfStream;
        }
    }
}
/*

Expresión Regular: Método Formal que a través de una secuencia de caracteres que define un PATRÓN de búsqueda

a) Reglas BNF 
b) Reglas BNF extendidas
c) Operaciones aplicadas al lenguaje

----------------------------------------------------------------

OAL

1. Concatenación simple (·)
2. Concatenación exponencial (Exponente) 
3. Cerradura de Kleene (*)
4. Cerradura positiva (+)
5. Cerradura Epsilon (?)
6. Operador OR (|)
7. Paréntesis ( y )

L = {A, B, C, D, E, ... , Z | a, b, c, d, e, ... , z}

D = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}

1. L.D
    LD
    >=

2. L^3 = LLL
    L^3D^2 = LLLDD
    D^5 = DDDDD
    =^2 = ==

3. L* = Cero o más letras
    D* = Cero o más dígitos

4. L+ = Una o más letras
    D+ = Una o más dígitos

5. L? = Cero o una letra (la letra es optativa-opcional)

6. L | D = Letra o dígito
    + | - = más o menos

7. (L D) L? (Letra seguido de un dígito y al final una letra opcional)

Producción gramátical

Clasificación del Token -> Expresión regular

Identificador -> L (L | D)*

Número -> D+ (.D+)? (E(+|-)? D+)?
FinSentencia -> ;
InicioBloque -> {
FinBloque -> }
OperadorTernario -> ?

Puntero -> ->

OperadorTermino -> + | -
IncrementoTermino -> ++ | += | -- | -=

Término+ -> + (+ | =)?
Término- -> - (- | = | >)?

OperadorFactor -> * | / | %
IncrementoFactor -> *= | /= | %=

Factor -> * | / | % (=)?

OperadorLogico -> && | || | !

NotOpRel -> ! (=)?

Asignación -> =

AsgOpRel -> = (=)?

OperadorRelacional -> > (=)? | < (> | =)? | == | !=

Cadena -> "c*"
Carácter -> 'c' | #D* | Lamda

----------------------------------------------------------------

Autómata: Modelo matemático que representa una expresión regular a través de un GRAFO, 
para una maquina de estado finito, para una máquina de estado finito que consiste en 
un conjunto de estados bien definidos:

- Un estado inicial 
- Un alfabeto de entrada 
- Una función de transición 

*/