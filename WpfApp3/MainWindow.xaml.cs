using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp3
{
    public partial class MainWindow : Window
    {
        private TextBox[,]? matrixABoxes;
        private TextBox[,]? matrixBBoxes;

        // Ссылки на гриды для удобства (создаются заново при Generate)
        private Grid? gridARef;
        private Grid? gridBRef;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            MatrixGrid.Children.Clear();

            if (!int.TryParse(RowsA.Text, out int rowsA) ||
                !int.TryParse(ColsA.Text, out int colsA) ||
                !int.TryParse(RowsB.Text, out int rowsB) ||
                !int.TryParse(ColsB.Text, out int colsB))
            {
                MessageBox.Show("Введите корректные целые числа для размеров матриц.");
                return;
            }

            if (colsA != rowsB)
            {
                MessageBox.Show("Количество столбцов A должно равняться количеству строк B.");
                return;
            }

            // Создаём новые гриды — чтобы не было конфликтов родителя
            var (newGridA, boxesA) = CreateMatrixGrid(rowsA, colsA);
            var (newGridB, boxesB) = CreateMatrixGrid(rowsB, colsB);

            // Сохраняем ссылки
            matrixABoxes = boxesA;
            matrixBBoxes = boxesB;
            gridARef = newGridA;
            gridBRef = newGridB;

            // Формируем визуал: A × B
            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(newGridA);
            stack.Children.Add(new TextBlock { Text = " × ", VerticalAlignment = VerticalAlignment.Center, FontSize = 24 });
            stack.Children.Add(newGridB);

            MatrixGrid.Children.Add(stack);
        }

        /// <summary>
        /// Создаёт новый Grid с TextBox'ами и возвращает Grid и массив TextBox
        /// </summary>
        private (Grid grid, TextBox[,] boxes) CreateMatrixGrid(int rows, int cols)
        {
            var grid = new Grid();

            for (int i = 0; i < rows; i++) grid.RowDefinitions.Add(new RowDefinition());
            for (int j = 0; j < cols; j++) grid.ColumnDefinitions.Add(new ColumnDefinition());

            var boxes = new TextBox[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var box = new TextBox
                    {
                        Width = 50,
                        Height = 25,
                        Margin = new Thickness(2),
                        Text = "0",
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    Grid.SetRow(box, i);
                    Grid.SetColumn(box, j);
                    grid.Children.Add(box);
                    boxes[i, j] = box;
                }
            }

            return (grid, boxes);
        }

        private void Multiply_Click(object sender, RoutedEventArgs e)
        {
            if (matrixABoxes == null || matrixBBoxes == null || gridARef == null || gridBRef == null)
            {
                MessageBox.Show("Сначала сгенерируйте матрицы.");
                return;
            }

            int rowsA = matrixABoxes.GetLength(0);
            int colsA = matrixABoxes.GetLength(1);
            int rowsB = matrixBBoxes.GetLength(0);
            int colsB = matrixBBoxes.GetLength(1);

            var A = new double[rowsA, colsA];
            var B = new double[rowsB, colsB];

            try
            {
                for (int i = 0; i < rowsA; i++)
                    for (int j = 0; j < colsA; j++)
                        A[i, j] = double.Parse(matrixABoxes[i, j].Text);

                for (int i = 0; i < rowsB; i++)
                    for (int j = 0; j < colsB; j++)
                        B[i, j] = double.Parse(matrixBBoxes[i, j].Text);
            }
            catch (FormatException)
            {
                MessageBox.Show("Одна или несколько ячеек содержат нечисловые значения.");
                return;
            }

            // Умножение
            var C = new double[rowsA, colsB];
            for (int i = 0; i < rowsA; i++)
                for (int j = 0; j < colsB; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < colsA; k++) sum += A[i, k] * B[k, j];
                    C[i, j] = sum;
                }

            // Создаём новый grid для результата
            var gridC = new Grid();
            for (int i = 0; i < rowsA; i++) gridC.RowDefinitions.Add(new RowDefinition());
            for (int j = 0; j < colsB; j++) gridC.ColumnDefinitions.Add(new ColumnDefinition());
            for (int i = 0; i < rowsA; i++)
            {
                for (int j = 0; j < colsB; j++)
                {
                    var tb = new TextBlock
                    {
                        Text = C[i, j].ToString("0.##"),
                        Width = 60,
                        Height = 25,
                        Margin = new Thickness(2),
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetRow(tb, i);
                    Grid.SetColumn(tb, j);
                    gridC.Children.Add(tb);
                }
            }

            // Перед добавлением — отсоединяем гриды от предыдущих родителей (если остались)
            DetachFromParent(gridARef);
            DetachFromParent(gridBRef);
            DetachFromParent(gridC);

            // Показываем A × B = C
            MatrixGrid.Children.Clear();
            var stack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            stack.Children.Add(gridARef);
            stack.Children.Add(new TextBlock { Text = " × ", FontSize = 24, VerticalAlignment = VerticalAlignment.Center });
            stack.Children.Add(gridBRef);
            stack.Children.Add(new TextBlock { Text = " = ", FontSize = 24, VerticalAlignment = VerticalAlignment.Center });
            stack.Children.Add(gridC);
            MatrixGrid.Children.Add(stack);
        }

        /// <summary>
        /// Если элемент уже имеет родителя (Panel), удаляет его оттуда.
        /// Это предотвращает InvalidOperationException при добавлении элемента в новый контейнер.
        /// </summary>
        private static void DetachFromParent(UIElement element)
        {
            if (element == null) return;
            if (element is FrameworkElement fe && fe.Parent is Panel parentPanel)
            {
                parentPanel.Children.Remove(element);
            }
            else if (element is FrameworkElement fe2 && fe2.Parent is ContentControl contentParent)
            {
                contentParent.Content = null;
            }
        }
    }
}
