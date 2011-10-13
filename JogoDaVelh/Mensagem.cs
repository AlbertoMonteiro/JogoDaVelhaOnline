using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JogoDaVelha
{
    [Serializable]
    public class Mensagem
    {
        public Mensagem(string nomeJogador)
        {
            this.NomeJogador = nomeJogador;
        }
        public KeyValuePair<int,int> Posicao { get; set; }
        public bool EuVenci { get; set; }
        public TipoMensagem Tipo { get; set; }
        public string NomeJogador { get; set; }
    }
}
