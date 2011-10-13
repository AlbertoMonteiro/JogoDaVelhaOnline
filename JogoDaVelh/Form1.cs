using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace JogoDaVelha
{
    public partial class Form1 : Form
    {
        private Label[,] matriz;
        private IEnumerable<Control> labels;
        private Comunicador comunicador;
        private bool aguardandoResposta;
        private bool online;
        private string meuSimbolo;

        public Form1()
        {
            InitializeComponent();
        }

        private void FormLoad(object sender, EventArgs e)
        {
            matriz = new Label[3, 3];
            labels = this.Controls.Cast<Control>().Where(controle =>
                controle is Label &&
                controle != lblMensagem &&
                controle != label1 &&
                controle != label2 &&
                controle != label3);

            var contador = 0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    matriz[i, j] = (Label)labels.ElementAt(contador++);

            meuSimbolo = "X";
            txtNome.Text = Environment.MachineName;
        }

        private void QuadradoSelecionado(object sender, EventArgs e)
        {
            var lbl = (Label)sender;
            if (string.IsNullOrWhiteSpace(lbl.Text))
            {
                lbl.Text = meuSimbolo;
                bool resultado = AlguemVenceu();
                if (online)
                {
                    int i = 0, j = 0;
                    bool found = false;
                    for (; i < 3; i++)
                    {
                        for (j = 0; j < 3; j++)
                        {
                            found = matriz[i, j] == lbl;
                            if (found) break;
                        }
                        if (found) break;
                    }
                    comunicador.InformarJogada(i, j, txtNome.Text, resultado);
                    LiberaQuadrados(false);
                    if (resultado)
                        MessageBox.Show("Parabéns otário", "Jogo da velha", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (!online)
                    if (!resultado)
                    {
                        TurnoDaMaquina();
                        if (AlguemVenceu())
                            MessageBox.Show("Se fudeu otário", "Jogo da velha", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                        MessageBox.Show("Parabéns otário", "Jogo da velha", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                MessageBox.Show("Escolha outro quadrado", "Jogo da velha", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool AlguemVenceu()
        {
            var linha = ChecaLinhasOuColunas(true);
            var coluna = ChecaLinhasOuColunas(false);

            var r3 = (lbl1.Text == lbl5.Text && lbl5.Text == lbl9.Text && !string.IsNullOrWhiteSpace(lbl1.Text));//diagonal
            var r8 = (lbl3.Text == lbl5.Text && lbl5.Text == lbl7.Text && !string.IsNullOrWhiteSpace(lbl3.Text));//diagonal

            return linha || coluna || r3 || r8;
        }

        private void TurnoDaMaquina()
        {
            var disponiveis = this.Controls.Cast<Control>().Where(controle =>
                controle is Label &&
                string.IsNullOrWhiteSpace(controle.Text) &&
                controle != lblMensagem &&
                controle != label1 &&
                controle != label2 &&
                controle != label3);
            Random random = new Random();

            var indice = random.Next(disponiveis.Count());

            disponiveis.ElementAt(indice).Text = "O";
        }

        private bool ChecaLinhasOuColunas(bool ehLinha)
        {
            var resultado = true;

            foreach (var item in new[] { 0, 1, 2 })
            {
                resultado = true;
                int i = 0;
                for (; i < 2; i++)
                    resultado = (ehLinha) ?
                        resultado && matriz[item, i].Text == matriz[item, i + 1].Text && !string.IsNullOrWhiteSpace(matriz[i, item].Text) :
                        resultado && matriz[i, item].Text == matriz[i + 1, item].Text && !string.IsNullOrWhiteSpace(matriz[i, item].Text);
                if (resultado && i <= 2)
                    break;
            }

            return resultado;
        }

        private void TipoDeJogoAlterado(object sender, EventArgs e)
        {
            panel1.Visible = rbOnline.Checked;
            if (rbOnline.Checked)
            {
                if (comunicador == null)
                {
                    comunicador = new Comunicador();
                    comunicador.QuandoReceberMensagem += RecebiMensagem;
                }
            }
        }

        private void RecebiMensagem(Mensagem mensagem)
        {
            if (mensagem.Tipo == TipoMensagem.JogadaEfetuada)
            {
                int x = mensagem.Posicao.Key, y = mensagem.Posicao.Value;
                PreencheQuadrado(x, y);
                if (mensagem.EuVenci)
                {
                    MessageBox.Show(string.Format("Se fudeu otário\n{0} ganhou de você.", mensagem.NomeJogador), "Jogo da velha", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LiberaQuadrados(false);
                }
                else
                    LiberaQuadrados(true);
            }
            else if (mensagem.Tipo == TipoMensagem.SolicitacaoDeJogo)
            {
                var retorno = MessageBox.Show(string.Format("{0} deseja jogar com você!\nVocê aceita?", mensagem.NomeJogador), "Jogo da Velha", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (retorno == DialogResult.Yes)
                {
                    comunicador.AceitarJogo(true, txtNome.Text);
                    IniciaJogo(true);
                    meuSimbolo = "O";
                    online = true;
                }
                else
                    comunicador.AceitarJogo(false, txtNome.Text);
            }
            else if (mensagem.Tipo == TipoMensagem.JogoAceito && aguardandoResposta)
            {
                aguardandoResposta = false;
                MessageBox.Show(string.Format("{0} aceitou o desafio", mensagem.NomeJogador), "Jogo da Velha");
                online = true;
            }
            else if (mensagem.Tipo == TipoMensagem.JogoRecusado && aguardandoResposta)
            {
                aguardandoResposta = false;
                MessageBox.Show(string.Format("{0} recusou o desafio", mensagem.NomeJogador), "Jogo da Velha");
                online = false;
            }
        }

        private void PreencheQuadrado(int x, int y)
        {
            Action a = () => matriz[x, y].Text = meuSimbolo == "X" ? "O" : "X";
            matriz[x, y].Invoke(a);
        }

        private void IniciarJogo(object sender, EventArgs e)
        {
            if (rbOnline.Checked)
            {
                comunicador.IniciaClient(txtIp.Text);
                comunicador.SolicitarJogo(txtNome.Text);
                aguardandoResposta = true;
            }
            else
            {
                IniciaJogo(false);
            }
        }

        private void IniciaJogo(bool online)
        {
            this.online = online;
            LiberaQuadrados(true);
            Action a = () => lblMensagem.Text = "Jogo iniciado";
            lblMensagem.Invoke(a);
        }

        private void LiberaQuadrados(bool liberar)
        {
            foreach (var item in labels)
            {
                Action a = () => item.Enabled = liberar;
                item.Invoke(a);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (comunicador != null)
                comunicador.Dispose();
        }
    }
}
