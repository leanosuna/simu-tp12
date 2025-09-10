namespace TP12
{
    internal class Program
    {
        static Sim simulador;

        static void Main(string[] args)
        {
            simulador = new Sim();
            simulador.Configurar();
            simulador.Iniciar();
        }
    }
}