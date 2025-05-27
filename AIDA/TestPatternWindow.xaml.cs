using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AIDA
{
    public partial class TestPatternWindow : Window
    {
        public TestPatternWindow(string patternType)
        {
            InitializeComponent();
            switch (patternType)
            {
                case "ColorBars":
                    CreateColorBars();
                    break;
                case "Grayscale":
                    CreateGrayscale();
                    break;
                case "Grid":
                    CreateGrid();
                    break;
                default:
                    break;
            }
        }

        private void CreateColorBars()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var redRect = new Rectangle { Fill = Brushes.Red };
            Grid.SetRow(redRect, 0);
            Grid.SetColumn(redRect, 0);
            grid.Children.Add(redRect);

            var greenRect = new Rectangle { Fill = Brushes.Green };
            Grid.SetRow(greenRect, 0);
            Grid.SetColumn(greenRect, 1);
            grid.Children.Add(greenRect);

            var blueRect = new Rectangle { Fill = Brushes.Blue };
            Grid.SetRow(blueRect, 0);
            Grid.SetColumn(blueRect, 2);
            grid.Children.Add(blueRect);

            this.Content = grid;
        }

        private void CreateGrayscale()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var lightGrayRect = new Rectangle { Fill = Brushes.LightGray };
            Grid.SetRow(lightGrayRect, 0);
            Grid.SetColumn(lightGrayRect, 0);
            grid.Children.Add(lightGrayRect);

            var grayRect = new Rectangle { Fill = Brushes.Gray };
            Grid.SetRow(grayRect, 0);
            Grid.SetColumn(grayRect, 1);
            grid.Children.Add(grayRect);

            var darkGrayRect = new Rectangle { Fill = Brushes.DarkGray };
            Grid.SetRow(darkGrayRect, 0);
            Grid.SetColumn(darkGrayRect, 2);
            grid.Children.Add(darkGrayRect);

            this.Content = grid;
        }

        private void CreateGrid()
        {
            var canvas = new Canvas();
            canvas.Width = 800;
            canvas.Height = 600;
            canvas.Background = Brushes.White;

            // Додаємо горизонтальні лінії
            for (int i = 0; i <= 600; i += 60)
            {
                var line = new Line
                {
                    X1 = 0,
                    Y1 = i,
                    X2 = 800,
                    Y2 = i,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                canvas.Children.Add(line);
            }

            // Додаємо вертикальні лінії
            for (int i = 0; i <= 800; i += 80)
            {
                var line = new Line
                {
                    X1 = i,
                    Y1 = 0,
                    X2 = i,
                    Y2 = 600,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                canvas.Children.Add(line);
            }

            this.Content = canvas;
        }
    }
}