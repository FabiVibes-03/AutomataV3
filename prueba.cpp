using System;

static void Main(string[] args)
{
    int x, y;
    x = 1;
    y = 1;

    do
    {
        do
        {
            Console.Write(x);  // Imprime el número x
            x++;  // Incrementa x
        }
        while (x <= y);  // La condición del ciclo interno es que x sea menor o igual a y

        Console.WriteLine();  // Salto de línea después de cada fila
        y++;  // Incrementa y para aumentar la cantidad de números impresos
        x = 1;  // Reinicia x para el próximo ciclo de la fila
    }
    while (y <= 5);  // El ciclo externo se repite hasta que y llegue a 5
}
