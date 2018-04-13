using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace JINS
{
    /// <summary>
    /// Interaction logic for BoardWindow.xaml
    /// </summary>
    public partial class BoardWindow : Window
    {
        private string currentRadioCheckedName = "11";
        public BoardWindow()
        {
            InitializeComponent();
        }

        private void centralize_click(object sender, RoutedEventArgs e)
        {
            radio_11.IsChecked = true;
            currentRadioCheckedName = "11";
        }

        public void SetRadio(string direction)
        {
            int row = int.Parse(currentRadioCheckedName[0].ToString());
            int column = int.Parse(currentRadioCheckedName[1].ToString());
            if (direction == "left" && column>0)
            {
                column--;
            }
            if(direction== "right" && column < 2)
            {
                column++;
            }
            if (direction == "up" && row > 0)
            {
                row--;
            }
            if (direction == "down" && row < 2)
            {
                row++;
            }
            currentRadioCheckedName = string.Concat(row, column);

            switch (currentRadioCheckedName)
            {
                case "00":
                    {
                        radio_00.IsChecked = true;
                        break;
                    }
                case "01":
                    {
                        radio_01.IsChecked = true;
                        break;
                    }
                case "02":
                    {
                        radio_02.IsChecked = true;
                        break;
                    }

                case "10":
                    {
                        radio_10.IsChecked = true;
                        break;
                    }
                case "11":
                    {
                        radio_11.IsChecked = true;
                        break;
                    }
                case "12":
                    {
                        radio_12.IsChecked = true;
                        break;
                    }

                case "20":
                    {
                        radio_20.IsChecked = true;
                        break;
                    }
                case "21":
                    {
                        radio_21.IsChecked = true;
                        break;
                    }
                case "22":
                    {
                        radio_22.IsChecked = true;
                        break;
                    }

            }
        }
    }
}
