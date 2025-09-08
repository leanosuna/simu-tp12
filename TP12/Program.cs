namespace TP12
{
    internal class Program
    {
        static Simulador simulador;

        static void Main(string[] args)
        {
            simulador = new Simulador();
            simulador.Configurar();
            simulador.Iniciar();
        }
    }
}