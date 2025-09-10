namespace TP12
{
    internal class MarcaTarjeta
    {
        int cantidadProcesos;
        public int N = 0;
        public int SumaAdquirientes = 0;
        double[] TC;
        public double[] STO;
        public double RechazadosTimeout = 0;
        public double SumaMontoRechazadosTimeout = 0;
        public double SumaDemora;
        public double ContadorEcomerce = 0;
        internal MarcaTarjeta(int cantidadProcesos)
        {
            this.cantidadProcesos = cantidadProcesos;
            TC = new double[cantidadProcesos];
            STO = new double[cantidadProcesos];
        }

        internal void Ejecutar(double T)
        {
            N++;
            // Obtengo el indice mas chico
            var idx = IndiceMin();
            var TCMayor = T < TC[idx];

            // Si hubo ocio, se lo sumo a ese proceso
            if (!TCMayor)
            {
                STO[idx] += T - TC[idx];
            }

            // Validacion de terminales ecomerce
            var tipoTX = Sim.RandomTipoTX();
            if (tipoTX == TipoTX.Ecomerce)
            {
                ContadorEcomerce++;
                //Sim.ContadorTerminales++;
                if (Sim.ContadorTerminales >= Sim.TerminalesMax)
                {
                    Sim.RechazadosPorFaltaTerminales++;
                    Sim.SumaMontoRechazadosTerminales += Sim.FDP_Monto() * Sim.RandomPorcACobrar();

                    TC[idx] += 10000;

                    return;
                }
            }

            // Verifico resolucion
            var resolucion = Sim.RandomResolucionTransaccion();

            double TA = 0;
            if (resolucion == Resolucion.Emisor)
            {
                TA = Sim.FDP_TAE();
                TC[idx] = TA + (TCMayor ? TC[idx] : T);


                if (Sim.Random() <= .9)
                {
                    if (tipoTX != TipoTX.Ecomerce)
                        return;

                    Sim.ContadorTerminales++;

                    return;
                }

                Sim.RechazadosAprobacionSistema++;
                return;
            }
            // adquiriente
            SumaAdquirientes++;
            TA = Sim.FDP_TAA();
            var prevTc = TC[idx];
            TC[idx] = TA + (TCMayor ? prevTc : T);

            //var diff = TC[idx] - T;
            var diff = TC[idx] - (TCMayor ? prevTc : T);
            SumaDemora += diff;

            if ((diff) < 800000) //4 seg
            {
                return;
            }

            RechazadosTimeout++;
            SumaMontoRechazadosTimeout += Sim.FDP_Monto() * Sim.RandomPorcACobrar();

        }
        public void Resultados()
        {
            Console.ForegroundColor = ConsoleColor.White;

            var pTimeout = (RechazadosTimeout / SumaAdquirientes);
            Console.Write("Timeouts: ");
            Console.Write($"{Sim.ValorConColorPorcentaje(pTimeout)} % ({RechazadosTimeout})\n");
            Console.ForegroundColor = ConsoleColor.White;

            var pPerdida = SumaMontoRechazadosTimeout * 10e6 / Sim.T;
            Console.Write($"Perdida promedio diario por Timeouts: ");
            Console.Write($"{Sim.ValorConColorPorcentaje(pPerdida)} [$ por seg]\n");
            Console.ForegroundColor = ConsoleColor.White;

            double sum = 0;
            foreach (var s in STO)
            {
                sum += s / Sim.T;
            }
            //TODO: siempre 0 aun con 1 solo proceso?
            var pPTO = sum / cantidadProcesos;
            //var pPTO = (STO.Sum() / Sim.T) / cantidadProcesos;

            Console.Write($"PTO (Promedio): ");
            Console.Write($"{Sim.ValorConColorPorcentaje(pPTO)} seg\n");


            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine();
        }


        int IndiceMin()
        {
            double min = double.MaxValue;
            int ret = 0;
            for (int i = 0; i < TC.Length; i++)
            {
                if (TC[i] < min)
                {
                    min = TC[i];
                    ret = i;
                }
            }
            return ret;
        }

    }
}
