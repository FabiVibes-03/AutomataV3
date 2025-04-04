using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Emulador
{
    public class Lenguaje : Sintaxis
    {

        private int ifCounter, whileCounter, doWhileCounter, forCounter, msgCounter;
        public bool isConsoleRead;
        Stack<float> s;
        List<Variable> l;
        Variable.TipoDato maximoTipo;
        bool huboCasteo = false;
        Variable.TipoDato tipoCasteo = Variable.TipoDato.Char;
        private List<string> listaMensajes = new List<string>();

        //SECTION - CONSTRUCTORES
        public Lenguaje(string nombre) : base(nombre)
        {
            s = new Stack<float>();
            l = new List<Variable>();
            log.WriteLine("Constructor lenguaje");
            maximoTipo = Variable.TipoDato.Char;
            ifCounter = whileCounter = doWhileCounter = forCounter = msgCounter = 1;
            isConsoleRead = false;
        }
        //!SECTION

        //SECTION - displayStack
        private void displayStack()
        {
            Console.WriteLine("Contenido del stack: ");
            foreach (float elemento in s)
            {
                Console.WriteLine(elemento);
            }
        }
        //!SECTION

        //SECTION - displayLista
        private void displayLista()
        {
            log.WriteLine("Lista de variables: ");
            foreach (Variable elemento in l)
            {
                switch (elemento.Tipo)
                {
                    //NOTE - Requerimiento 1
                    case Variable.TipoDato.Char:
                        log.WriteLine($"{elemento.Nombre} {elemento.Tipo} {elemento.Valor}");
                        break;
                    case Variable.TipoDato.Int:
                        log.WriteLine($"{elemento.Nombre} {elemento.Tipo} {elemento.Valor}");
                        break;
                    case Variable.TipoDato.Float:
                        log.WriteLine($"{elemento.Nombre} {elemento.Tipo} {elemento.Valor}");
                        break;
                }
            }
            foreach (string elemento in listaMensajes)
            {
            }
        }
        //!SECTION

        //SECTION - Programa
        //Programa  -> Librerias? Variables? Main
        public void Programa()
        {
            if (Contenido == "using")
            {
                Librerias();
            }
            if (Clasificacion == Tipos.TipoDato)
            {
                Variables();
            }
            Main();
            displayLista();
        }
        //Librerias -> using ListaLibrerias; Librerias?
        //!SECTION

        //SECTION - Librerias
        private void Librerias()
        {
            match("using");
            ListaLibrerias();
            match(";");
            if (Contenido == "using")
            {
                Librerias();
            }
        }
        //Variables -> tipo_dato Lista_identificadores; Variables?
        //!SECTION

        //SECTION - Variables
        private void Variables()
        {
            Variable.TipoDato t = Variable.TipoDato.Char;
            switch (Contenido)
            {
                case "int": t = Variable.TipoDato.Int; break;
                case "float": t = Variable.TipoDato.Float; break;
            }
            match(Tipos.TipoDato);
            ListaIdentificadores(t);
            match(";");
            if (Clasificacion == Tipos.TipoDato)
            {
                Variables();
            }
        }
        //!SECTION
        //SECTION - ListaLibrerias
        //ListaLibrerias -> identificador (.ListaLibrerias)?
        private void ListaLibrerias()
        {
            match(Tipos.Identificador);
            if (Contenido == ".")
            {
                match(".");
                ListaLibrerias();
            }
        }
        //!SECTION
        //SECTION - ListaIdentificadores
        //ListaIdentificadoress -> identificador (= Expresion)? (,ListaIdentificadores)?
        private void ListaIdentificadores(Variable.TipoDato t)
        {
            if (l.Find(variable => variable.Nombre == Contenido) != null)
            {
                throw new Error($"La variable {Contenido} ya existe", log, linea, columna);
            }

            Variable v = new Variable(t, Contenido);
            l.Add(v);

            match(Tipos.Identificador);
            if (Contenido == "=")
            {
                match("=");
                if (Contenido == "Console")
                {
                    match("Console");
                    match(".");
                    if (Contenido == "Read")
                    {
                        match("Read");
                        int r = Console.Read();
                        v.setValor(r, linea, columna, log, maximoTipo); // Asignamos el último valor leído a la última variable detectada
                    }
                    else
                    {
                        match("ReadLine");
                        string? r = Console.ReadLine();
                        float result;

                        if (float.TryParse(r, out result))
                        {
                            v.setValor(result, linea, columna, log, maximoTipo);
                        }
                        else
                        {
                            throw new Error("Sintaxis. No se ingresó un número ", log, linea, columna);
                        }
                    }
                    match("(");
                    match(")");

                }
                else
                {
                    // Como no se ingresó un número desde el Console, entonces viene de una expresión matemática
                    Expresion();
                    float resultado = s.Pop();
                    //NOTE - REVISAR ESTE POP
                    v.setValor(resultado, linea, columna, log, maximoTipo);
                }
            }
            if (Contenido == ",")
            {
                match(",");
                ListaIdentificadores(t);
            }
        }
        //!SECTION
        //SECTION - BloqueInstrucciones
        //BloqueInstrucciones -> { listaIntrucciones? }
        private void BloqueInstrucciones(bool ejecuta)
        {
            match("{");

            while (Contenido != "}")
            {
                //Console.WriteLine($"→ [BloqueInstrucciones] Contenido != , Contenido = '{Contenido}'");
                Instruccion(ejecuta);
            }

            //Console.WriteLine($"→ [BloqueInstrucciones] Antes de match(signo), Contenido = '{Contenido}'");
            match("}"); // Consume la llave de cierre

            // Avanza al token siguiente después de cerrar el bloque.
            //nextToken();
        }



        //!SECTION
        //SECTION - ListaInstrucciones
        //ListaInstrucciones -> Instruccion ListaInstrucciones?
        private void ListaInstrucciones(bool ejecuta)
        {
            Instruccion(ejecuta);
            if (Contenido != "}")
            {
                ListaInstrucciones(ejecuta);
            }
            else
            {
                match("}");
            }
        }
        //!SECTION
        //SECTION - Instruccion
        //Instruccion -> console | If | While | do | For | Variables | Asignación
        private void Instruccion(bool ejecuta)
        {
            if (Contenido == "")
            {
                // Evita ejecutar instrucciones vacías (token perdido o mal manejo de seek)
                return;
            }

            switch (Contenido)
            {
                case "Console":
                    console(ejecuta);
                    //match(";");
                    break;
                case "if":
                    If(ejecuta);
                    break;
                case "while":
                    While(ejecuta);
                    break;
                case "do":
                    int posTMP = Math.Max(0, characterCounter - Contenido.Length);
                    int lineaTMP = linea;
                    Do(ejecuta, posTMP, lineaTMP);
                    break;
                case "for":
                    For(ejecuta);
                    break;
                default:
                    if (Clasificacion == Tipos.TipoDato)
                    {
                        Variables();
                    }
                    else
                    {
                        Asignacion(ejecuta);
                        match(";"); // ← ESTA LÍNEA DEBE ESTAR
                    }
                    break;
            }
        }

        //!SECTION
        //Asignacion -> Identificador = Expresion; (DONE)
        /*
        Id++ (DONE)
        Id-- (DONE)
        Id IncrementoTermino Expresion (DONE)
        Id IncrementoFactor Expresion (DONE)
        Id = Console.Read() (DONE)
        Id = Console.ReadLine() (DONE)
        */

        //SECTION - Asignacion
        private void Asignacion(bool execute)
        {
            //Console.WriteLine($"→ [Asignacion] Token actual: '{Contenido}'");

            huboCasteo = false;
            tipoCasteo = Variable.TipoDato.Char;
            maximoTipo = Variable.TipoDato.Char;

            float r;
            Variable? v = l.Find(variable => variable.Nombre == Contenido);
            if (v == null)
            {
                throw new Error($"Sintaxis: La variable {Contenido} no está definida", log, linea, columna);
            }
            //Console.Write(Contenido + " = ");
            match(Tipos.Identificador);
            //NOTE - Requerimiento 2
            switch (Contenido)
            {
                case "++":
                    match("++");
                    r = v.Valor + 1;
                    v.setValor(r, linea, columna, log, maximoTipo);
                    break;
                case "--":
                    match("--");
                    r = v.Valor - 1;
                    v.setValor(r, linea, columna, log, maximoTipo);
                    break;
                case "=":
                    match("=");
                    if (Contenido == "Console")
                    {
                        bool console = false;
                        bool isRead = false;
                        string content = "";
                        match("Console");
                        match(".");

                        switch (Contenido)
                        {
                            case "Write":
                                console = true;
                                match("Write");
                                break;
                            case "Read":
                                isRead = true;
                                match("Read");
                                break;
                            case "ReadLine":
                                isRead = true;
                                match("ReadLine");
                                break;
                            default:
                                match("WriteLine");
                                break;
                        }

                        match("(");

                        if (!isRead && Contenido != ")")
                        {

                            if (Clasificacion == Tipos.Cadena)
                            {
                                if (execute)
                                {
                                    Console.Write(Contenido.ToString().Replace('"', ' '));
                                }
                                match(Tipos.Cadena);
                            }
                            else
                            {
                                string nomV = Contenido;
                                match(Tipos.Identificador);
                                v = l.Find(variable => variable.Nombre == nomV);

                                if (v == null)
                                {
                                    throw new Error("La variable no existe", log, linea, columna);
                                }
                                if (execute)
                                {
                                    //? Por alguna razón sigue imprimiendo en float REVISAR
                                    Console.Write(((int)v.Valor).ToString());
                                }
                                //match(v.Valor.ToString());
                            }
                        }

                        if (Contenido == "+")
                        {
                            match("+");
                            Concatenaciones();
                        }

                        match(")");
                        if (isRead) //Esto si
                        {
                            if (Contenido == "ReadLine")
                            {
                                content = Console.ReadLine();
                            }
                            else
                            {
                                content = ((char)Console.Read()).ToString();
                            }
                            content = Contenido == "ReadLine" ? Console.ReadLine() : ((char)Console.Read()).ToString();

                        }


                        if (!isRead && execute)
                        {
                            switch (console)
                            {
                                case true: Console.Write(content); break;
                                case false: Console.WriteLine(content); break;
                            }
                        }

                    }
                    else
                    {
                        Expresion();
                        r = s.Pop();
                        maximoTipo = Variable.valorTipoDato(r, maximoTipo, huboCasteo);
                        v.setValor(r, linea, columna, log, maximoTipo);
                    }
                    break;
                case "+=":
                    match("+=");
                    Expresion();
                    r = v.Valor + s.Pop();
                    maximoTipo = Variable.valorTipoDato(r, maximoTipo, huboCasteo);
                    v.setValor(r, linea, columna, log, maximoTipo);
                    break;
                case "-=":
                    match("-=");
                    Expresion();
                    r = v.Valor - s.Pop();
                    maximoTipo = Variable.valorTipoDato(r, maximoTipo, huboCasteo);
                    v.setValor(r, linea, columna, log, maximoTipo);
                    break;
                case "*=":
                    match("*=");
                    Expresion();
                    r = v.Valor * s.Pop();
                    maximoTipo = Variable.valorTipoDato(r, maximoTipo, huboCasteo);
                    v.setValor(r, linea, columna, log, maximoTipo);
                    break;
                case "/=":
                    match("/=");
                    Expresion();
                    r = v.Valor / s.Pop();
                    maximoTipo = Variable.valorTipoDato(r, maximoTipo, huboCasteo);
                    v.setValor(r, linea, columna, log, maximoTipo);
                    break;
                case "%=":
                    match("%=");
                    Expresion();
                    r = v.Valor % s.Pop();
                    maximoTipo = Variable.valorTipoDato(r, maximoTipo, huboCasteo);
                    v.setValor(r, linea, columna, log, maximoTipo);
                    break;
            }
            //displayStack();
        }

        //!SECTION

        //SECTION - IF
        /*If -> if (Condicion) bloqueInstrucciones | instruccion
        (else bloqueInstrucciones | instruccion)?*/
        private void If(bool execute2)
        {
            match("if");
            match("(");

            string label = $"jump_if_else_{ifCounter}";
            string labelEndIf = $"jump_end_if_{ifCounter}";
            ifCounter++;

            bool execute = Condicion() && execute2;
            match(")");
            if (Contenido == "{")
            {
                BloqueInstrucciones(execute);
            }
            else
            {
                Instruccion(execute);
            }

            if (Contenido == "else")
            {
                match("else");
                bool executeElse = !execute; // Solo se ejecuta el else si el if no se ejecutó
                if (Contenido == "{")
                {
                    BloqueInstrucciones(executeElse);
                }
                else
                {
                    Instruccion(executeElse);
                }
            }
        }
        //!SECTION
        //SECTION - Condicion
        //Condicion -> Expresion operadorRelacional Expresion
        private bool Condicion(bool isDo = false)
        {
            maximoTipo = Variable.TipoDato.Char;
            float valor1 = ExpresionValor();  // Evalúa la primera expresión

            string operador = Contenido;
            match(Tipos.OperadorRelacional);  // Consume el operador relacional

            maximoTipo = Variable.TipoDato.Char;
            float valor2 = ExpresionValor();  // Evalúa la segunda expresión

            switch (operador)
            {
                case ">": return valor1 > valor2;
                case ">=": return valor1 >= valor2;
                case "<": return valor1 < valor2;
                case "<=": return valor1 <= valor2;
                case "==": return valor1 == valor2;
                default: return valor1 != valor2;
            }
        }


        //!SECTION
        //SECTION - While
        //While -> while(Condicion) bloqueInstrucciones | instruccion
        private void While(bool execute)
        {
            match("while");
            match("(");

            string inicioWhile = $"while_{whileCounter}";
            string finWhile = $"end_while_{whileCounter}";
            whileCounter++;
            bool condicionValida = Condicion();
            match(")");
            if (Contenido == "{")
            {
                BloqueInstrucciones(condicionValida);
            }
            else
            {
                Instruccion(condicionValida);
            }

        }
        //!SECTION
        //SECTION - DO
        /*Do -> do bloqueInstrucciones | intruccion 
        while(Condicion);*/
        private void Do(bool execute, int charTMP, int LineaTMP)
        {
            //Console.WriteLine("→ [Do] Iniciando ciclo do-while");

            bool ejecutaDO;

            do
            {
                //Console.WriteLine("DEBUG: Iniciando ciclo do-while. Posición inicial del archivo: " + charTMP + ", línea: " + LineaTMP);
                //Console.WriteLine("DEBUG: Ingresando al ciclo do-while. Token antes de match('do'): " + Contenido);
                match("do");
                //Console.WriteLine("DEBUG: 'do' reconocido. Token actual tras match: " + Contenido);

                if (Contenido == "{")
                {
                    //Console.WriteLine("DEBUG: Se detecta inicio de bloque '{'. Ejecutando BloqueInstrucciones.");
                    BloqueInstrucciones(execute);
                }
                else
                {
                    //Console.WriteLine("DEBUG: No se detecta bloque, ejecutando Instruccion.");
                    Instruccion(execute);
                }

                //Console.WriteLine("DEBUG: Cuerpo del do-while ejecutado. Token actual: " + Contenido);
                //Console.WriteLine("DEBUG: Esperando 'while'. Token actual antes de match('while'): " + Contenido);
                match("while");
                //Console.WriteLine("DEBUG: 'while' reconocido. Token actual: " + Contenido);
                match("(");
                //Console.WriteLine("DEBUG: '(' reconocido. Evaluando condición do-while.");

                ejecutaDO = execute && Condicion();
                //Console.WriteLine("DEBUG: Resultado de la condición do-while: " + ejecutaDO);

                match(")");
                //Console.WriteLine("DEBUG: ')' reconocido. Finalizando evaluación de la condición.");
                match(";");
                //Console.WriteLine("DEBUG: ';' reconocido. Fin de la iteración actual del do-while.");

                if (ejecutaDO)
                {
                    //Console.WriteLine("DEBUG: Condición verdadera. Reiniciando ciclo do-while. Retornando a posición: " + charTMP + ", línea: " + LineaTMP);
                    archivo.DiscardBufferedData();
                    archivo.BaseStream.Seek(charTMP, SeekOrigin.Begin);
                    archivo.BaseStream.Position = charTMP;
                    characterCounter = charTMP;
                    linea = LineaTMP;
                    columna = 1;
                    nextToken(); // ← Esto debe volver a leer "do"
                    //Console.WriteLine("DEBUG: Ciclo reiniciado. Token actual: '" + Contenido + "', posición actual: " + characterCounter + ", línea: " + linea);
                }
                else
                {
                    //Console.WriteLine("DEBUG: Fin del ciclo do-while.");
                }

            } while (ejecutaDO);
        }


        //!SECTION
        //SECTION - For
        /*For -> for(Asignacion; Condicion; Asignacion) 
        BloqueInstrucciones | Intruccion*/
        private void For(bool execute)
        {
            //Console.WriteLine("→ [For] Iniciando ciclo for");

            match("for");
            match("(");

            //Console.WriteLine("→ [For] Asignación inicial");
            Asignacion(execute);
            match(";");

            // Guardamos posición de la condición
            long posCondicion = archivo.BaseStream.Position;
            int charCondicion = characterCounter;

            //Console.WriteLine("→ [For] Evaluando condición inicial");
            bool condicionValida = execute && Condicion();
            //Console.WriteLine($"→ [For] Condición inicial: {condicionValida}");
            match(";");

            string[] tokensIncremento = CapturarIncrementoTokens();
            match(")");

            nextToken(); // Avanzamos al primer token real del cuerpo del for

            long posCuerpo = archivo.BaseStream.Position;
            int charCuerpo = characterCounter;

            while (condicionValida)
            {
                //Console.WriteLine("→ [For] Ejecutando cuerpo del ciclo");

                archivo.DiscardBufferedData();
                archivo.BaseStream.Seek(posCuerpo, SeekOrigin.Begin);
                characterCounter = charCuerpo;
                nextToken();

                if (Contenido == "{")
                {
                    BloqueInstrucciones(execute);
                }
                else
                {
                    Instruccion(execute);
                }

                //Console.WriteLine("→ [For] Ejecutando incremento");
                ProcesarIncremento(execute, tokensIncremento);

                archivo.DiscardBufferedData();
                archivo.BaseStream.Seek(posCondicion, SeekOrigin.Begin);
                characterCounter = charCondicion;
                nextToken();

                condicionValida = execute && Condicion();
                //Console.WriteLine($"→ [For] Reevaluando condición: {condicionValida}");
                match(";");
            }

            // Si nunca entró al ciclo, debemos parsear el cuerpo para mantener lectura sincronizada
            if (!condicionValida)
            {
                //Console.WriteLine("→ [For] Condición falsa desde el principio. Saltando cuerpo...");
                archivo.DiscardBufferedData();
                archivo.BaseStream.Seek(posCuerpo, SeekOrigin.Begin);
                characterCounter = charCuerpo;
                nextToken();

                if (Contenido == "{")
                {
                    BloqueInstrucciones(false);
                }
                else
                {
                    Instruccion(false);
                }
            }

            //Console.WriteLine("→ [For] Fin del ciclo");
        }



        //!SECTION
        //SECTION - Console
        private void console(bool execute)
        {
            bool isWrite = false;
            bool isWriteLine = false;
            bool isRead = false;

            //Console.WriteLine("DEBUG: Iniciando método console(). Token actual: " + Contenido);

            match("Console");
            match(".");

            switch (Contenido)
            {
                case "Write":
                    isWrite = true;
                    match("Write");
                    break;
                case "WriteLine":
                    isWriteLine = true;
                    match("WriteLine");
                    break;
                case "Read":
                    isRead = true;
                    match("Read");
                    break;
                case "ReadLine":
                    isRead = true;
                    match("ReadLine");
                    break;
                default:
                    throw new Error("Método de Console no soportado", log, linea, columna);
            }

            match("(");

            string resultado = "";

            if (!isRead && Contenido != ")")
            {
                //Console.WriteLine("DEBUG: Ingresando a análisis de argumentos. Token actual: " + Contenido + ", Clasificación: " + Clasificacion);

                if (Clasificacion == Tipos.Cadena)
                {
                    // Si comienza con cadena, tratamos como texto concatenado
                    resultado = ExpresionTexto();
                }
                else
                {
                    // Si no, evaluamos como expresión numérica y convertimos a string
                    float val = ExpresionValor();
                    resultado = val.ToString();
                }
            }

            match(")");
            match(";");

            //Console.WriteLine("DEBUG: Preparando salida. Resultado: " + resultado);

            if (execute)
            {
                if (isRead)
                {
                    Console.ReadLine(); // Por ahora no almacena
                }
                else
                {
                    Console.Write(resultado);
                    if (isWriteLine)
                        Console.WriteLine();
                }
            }
        }





        //!SECTION
        //SECTION - Concatenaciones
        // Concatenaciones -> Identificador|Cadena ( + concatenaciones )?
        private string Concatenaciones()
        {
            string resultado = "";
            // Caso: si el token actual es un identificador, se trata de una variable.
            if (Clasificacion == Tipos.Identificador)
            {
                Variable? v = l.Find(variable => variable.Nombre == Contenido);
                if (v != null)
                {
                    resultado = v.Valor.ToString(); // Obtener el valor de la variable
                }
                else
                {
                    throw new Error("La variable " + Contenido + " no está definida", log, linea, columna);
                }
                match(Tipos.Identificador);
            }
            // Caso: si el token es una cadena, se toma el literal.
            else if (Clasificacion == Tipos.Cadena)
            {
                resultado = Contenido.Trim('"');
                match(Tipos.Cadena);
            }
            // **Nuevo caso**: Si el token es "(", se evalúa la expresión completa.
            else if (Contenido == "(")
            {
                match("(");
                float valor = ExpresionValor();  // Evalúa la expresión entre paréntesis
                match(")");
                resultado = valor.ToString();
            }

            // Si después hay un operador +, se acumula la siguiente parte de la concatenación.
            if (Contenido == "+")
            {
                match("+");
                resultado += Concatenaciones();
            }

            return resultado;
        }

        //!SECTION
        //SECTION - Main
        //Main -> static void Main(string[] args) BloqueInstrucciones 
        private void Main()
        {
            match("static");
            match("void");
            match("Main");
            match("(");
            match("string");
            match("[");
            match("]");
            match("args");
            match(")");
            BloqueInstrucciones(true);
        }
        //!SECTION
        //SECTION - Expresion
        // Expresion -> Termino MasTermino
        private void Expresion()
        {
            Termino();
            MasTermino();
        }
        //!SECTION
        //SECTION - MasTermino
        //MasTermino -> (OperadorTermino Termino)?
        private void MasTermino()
        {
            if (Clasificacion == Tipos.OperadorTermino)
            {
                string operador = Contenido;
                match(Tipos.OperadorTermino);
                Termino();
                //Console.Write(operador + " ");
                float n1 = s.Pop();
                float n2 = s.Pop();
                float resultado = 0;
                switch (operador)
                {
                    case "+": resultado = n2 + n1; break;
                    case "-": resultado = n2 - n1; break;
                }

                Variable.TipoDato tipoResultado = Variable.valorTipoDato(resultado, maximoTipo, huboCasteo);
                //Si uno de los valores es float, el resultado sera float
                if (maximoTipo == Variable.TipoDato.Float || tipoResultado == Variable.TipoDato.Float)
                {
                    tipoResultado = Variable.TipoDato.Float;
                }

                if (huboCasteo)
                {
                    maximoTipo = tipoCasteo;
                }
                else
                {
                    if (maximoTipo == Variable.TipoDato.Float || tipoResultado == Variable.TipoDato.Float)
                    {
                        maximoTipo = Variable.TipoDato.Float;
                    }
                    else if (maximoTipo < tipoResultado)
                    {
                        maximoTipo = tipoResultado;
                    }
                }

                //Hacemos el push al final ya con el resultado
                s.Push(resultado);
            }
        }
        //!SECTION
        //SECTION - Termino
        //Termino -> Factor PorFactor
        private void Termino()
        {
            Factor();
            PorFactor();
        }
        //!SECTION
        //SECTION - PorFactor
        //PorFactor -> (OperadorFactor Factor)?
        private void PorFactor()
        {
            if (Clasificacion == Tipos.OperadorFactor)
            {
                string operador = Contenido;
                match(Tipos.OperadorFactor);
                Factor();
                //Console.Write(operador + " ");
                float n1 = s.Pop();
                float n2 = s.Pop();

                float resultado = 0;

                switch (operador)
                {
                    case "*": resultado = n2 * n1; break; //AX
                    case "/": resultado = n2 / n1; break; //AL
                    case "%": resultado = n2 % n1; break; //AH
                }
                Variable.TipoDato tipoResultado = Variable.valorTipoDato(resultado, maximoTipo, huboCasteo);
                if (maximoTipo == Variable.TipoDato.Float || tipoResultado == Variable.TipoDato.Float)
                {
                    tipoResultado = Variable.TipoDato.Float;
                }

                if (huboCasteo)
                {
                    maximoTipo = tipoCasteo;
                }
                else
                {
                    if (maximoTipo == Variable.TipoDato.Float || tipoResultado == Variable.TipoDato.Float)
                    {
                        maximoTipo = Variable.TipoDato.Float;
                    }
                    else if (maximoTipo < tipoResultado)
                    {
                        maximoTipo = tipoResultado;
                    }
                }

                s.Push(resultado);
            }
        }
        //!SECTION
        //SECTION - FACTOR
        //Factor -> numero | identificador | (Expresion)
        private void Factor()
        {
            //Console.WriteLine($"→ [Factor] Token recibido: '{Contenido}' - Clasificación: {Clasificacion}");

            maximoTipo = Variable.TipoDato.Char;

            // Caso 1: Si es un número
            if (Clasificacion == Tipos.Numero)
            {
                Variable? v = l.Find(variable => variable.Nombre == Contenido);
                //Contenido lo pasamos a valor
                float valor = float.Parse(Contenido);
                Variable.TipoDato tipoValor = Variable.valorTipoDato(valor, maximoTipo, huboCasteo);
                // Verifica el tipo de dato máximo necesario para el número
                if (maximoTipo < tipoValor)
                {
                    maximoTipo = tipoValor;
                }

                s.Push(valor);
                match(Tipos.Numero);
            }
            // Caso 2: Si es un identificador (variable)
            else if (Clasificacion == Tipos.Identificador)
            {
                // Busca la variable en la lista de variables
                Variable? v = l.Find(variable => variable.Nombre == Contenido);
                if (v == null)
                {
                    throw new Error("Sintaxis: la variable " + Contenido + " no está definida", log, linea, columna);
                }

                // Actualiza el tipo máximo si es necesario
                if (maximoTipo < v.Tipo)
                {
                    maximoTipo = v.Tipo;
                }

                // Agrega el valor de la variable al stack
                s.Push(v.Valor);
                match(Tipos.Identificador);
            }
            else if (Clasificacion == Tipos.FuncionMatematica)
            {
                string FuntionName = Contenido;
                match(Tipos.FuncionMatematica);
                match("(");
                Expresion();
                match(")");

                float resultado = s.Pop();
                float mathResult = mathFunction(resultado, FuntionName);
                s.Push(mathResult);
            }
            // Caso 3: Si es una expresión entre paréntesis
            else
            {
                match("(");
                //Casteo explicito
                huboCasteo = false;
                if (Clasificacion == Tipos.TipoDato)
                {
                    // Determina el tipo de casteo
                    switch (Contenido)
                    {
                        case "int": tipoCasteo = Variable.TipoDato.Int; break;
                        case "float": tipoCasteo = Variable.TipoDato.Float; break;
                        case "char": tipoCasteo = Variable.TipoDato.Char; break;
                    }

                    match(Tipos.TipoDato);
                    match(")");
                    match("(");
                    huboCasteo = true;
                }
                //!SECTION
                // Evalúa la expresión dentro de los paréntesis
                Expresion();
                if (huboCasteo)
                {
                    float valor = s.Pop();

                    switch (tipoCasteo)
                    {
                        case Variable.TipoDato.Int:
                            valor = valor % MathF.Pow(2, 16);
                            break;

                        case Variable.TipoDato.Char:
                            valor = valor % MathF.Pow(2, 8);
                            break;
                    }
                    //Obligamos el casteo
                    maximoTipo = tipoCasteo;
                    s.Push(valor);
                }
                match(")");
            }
        }
        // Método auxiliar token
        private string[] CapturarIncrementoTokens()
        {
            List<string> tokensIncremento = new List<string>();

            while (Contenido != ")")
            {
                tokensIncremento.Add(Contenido);
                nextToken();
            }
            return tokensIncremento.ToArray();
        }

        // Método auxiliar
        private void ProcesarIncremento(bool execute, string[] tokensIncremento)
        {
            //Console.WriteLine("→ [ProcesarIncremento] Tokens recibidos: " + string.Join(" ", tokensIncremento));

            if (tokensIncremento.Length == 2 && (tokensIncremento[1] == "++" || tokensIncremento[1] == "--"))
            {
                string varName = tokensIncremento[0];
                string op = tokensIncremento[1];

                //Console.WriteLine($"→ [ProcesarIncremento] Buscando variable '{varName}' en lista de variables");

                Variable? v = l.Find(variable => variable.Nombre == varName);
                if (v == null)
                {
                    //Console.WriteLine("→  [ProcesarIncremento] No se encontró la variable en la lista");
                    throw new Error($"Sintaxis: la variable {varName} no está definida", log, linea, columna);
                }

                if (execute)
                {
                    float r = v.Valor;
                    switch (op)
                    {
                        case "++": r += 1; break;
                        case "--": r -= 1; break;
                    }
                    //Console.WriteLine($"→ [ProcesarIncremento] Nuevo valor de '{varName}': {r}");
                    v.setValor(r, linea, columna, log, Variable.valorTipoDato(r, maximoTipo));
                }
            }
            else
            {
                // Soporta expresiones tipo: i += 2, i = i + 2, etc.
                //Console.WriteLine("→ [ProcesarIncremento] Incremento complejo detectado");

                // Ejemplo: tokens = ["i", "+=", "2"]
                if (tokensIncremento.Length >= 3)
                {
                    string varName = tokensIncremento[0];
                    string operador = tokensIncremento[1];
                    string valorStr = tokensIncremento[2];

                    Variable? v = l.Find(variable => variable.Nombre == varName);
                    if (v == null)
                    {
                        throw new Error($"Sintaxis: La variable {varName} no está definida", log, linea, columna);
                    }

                    float valor;

                    if (!float.TryParse(valorStr, out valor))
                    {
                        Variable? v2 = l.Find(variable => variable.Nombre == valorStr);
                        if (v2 == null)
                        {
                            throw new Error($"Sintaxis: Valor inválido o variable {valorStr} no definida", log, linea, columna);
                        }
                        valor = v2.Valor;
                    }

                    if (execute)
                    {
                        float resultado = v.Valor;

                        switch (operador)
                        {
                            case "+=": resultado += valor; break;
                            case "-=": resultado -= valor; break;
                            case "*=": resultado *= valor; break;
                            case "/=": resultado /= valor; break;
                            case "%=": resultado %= valor; break;
                            default:
                                throw new Error($"Operador no soportado en incremento: {operador}", log, linea, columna);
                        }

                        //Console.WriteLine($"→ [ProcesarIncremento] {varName} {operador} {valor} → {resultado}");
                        v.setValor(resultado, linea, columna, log, Variable.valorTipoDato(resultado, maximoTipo));
                    }
                }
                else
                {
                    throw new Error("Incremento no válido en el ciclo for", log, linea, columna);
                }
            }
        }


        private float mathFunction(float value, string name)
        {
            switch (name)
            {
                case "abs":
                    return MathF.Abs(value);
                case "ceil":
                    return MathF.Ceiling(value);
                case "pow":
                    return MathF.Pow(value, 2);
                case "sqrt":
                    return MathF.Sqrt(value);
                case "exp":
                    return MathF.Exp(value);
                case "floor":
                    return MathF.Floor(value);
                case "log10":
                    return MathF.Log10(value);
                case "log2":
                    return MathF.Log(value, 2);
                case "rand":
                    return new Random().Next((int)value);
                case "trunc":
                    return MathF.Truncate(value);
                case "round":
                    return MathF.Round(value);
                default:
                    throw new Error($"Función matemática '{name}' no está definida", log, linea, columna);
            }
        }

        private string ExpresionTexto()
        {
            string resultado = "";

            // Si comienza con una cadena, identificador o número
            if (Clasificacion == Tipos.Cadena)
            {
                resultado += Contenido.Trim('"');
                match(Tipos.Cadena);
            }
            else if (Clasificacion == Tipos.Identificador)
            {
                Variable? v = l.Find(variable => variable.Nombre == Contenido);
                if (v == null)
                    throw new Error("La variable no existe", log, linea, columna);
                resultado += v.Valor.ToString();
                match(Tipos.Identificador);
            }
            else if (Clasificacion == Tipos.Numero)
            {
                resultado += Contenido;
                match(Tipos.Numero);
            }
            else
            {
                float val = ExpresionValor();
                resultado += val.ToString();
            }

            // Soportar concatenaciones con +
            while (Contenido == "+")
            {
                match("+");
                if (Clasificacion == Tipos.Cadena)
                {
                    resultado += Contenido.Trim('"');
                    match(Tipos.Cadena);
                }
                else if (Clasificacion == Tipos.Identificador)
                {
                    Variable? v = l.Find(variable => variable.Nombre == Contenido);
                    if (v == null)
                        throw new Error("La variable no existe", log, linea, columna);
                    resultado += v.Valor.ToString();
                    match(Tipos.Identificador);
                }
                else if (Clasificacion == Tipos.Numero)
                {
                    resultado += Contenido;
                    match(Tipos.Numero);
                }
                else
                {
                    float val = ExpresionValor();
                    resultado += val.ToString();
                }
            }

            return resultado;
        }

        private float ExpresionValor()
        {
            Termino();
            MasTermino();
            // El resultado final de la expresión debe quedar en la cima de la pila
            return s.Pop();
        }

        /*SNT = Producciones = Invocar el metodo
        ST  = Tokens (Contenido | Classification) = Invocar match    Variables -> tipo_dato Lista_identificadores; Variables?*/
    }
}

//SECTION - Cambios de esta version
/*
   
*/
//!SECTION