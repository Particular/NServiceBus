using System;
using System.Windows.Forms;

namespace TimeoutManager
{
    public interface IStarbucksTimeoutManagerView
    {
        void Start();
    }

    public partial class StarbucksTimeoutManager : Form, IStarbucksTimeoutManagerView
    {
        public StarbucksTimeoutManager()
        {
            InitializeComponent();
        }

        public void Start()
        {
            Application.Run(this);
        }
    }
}
