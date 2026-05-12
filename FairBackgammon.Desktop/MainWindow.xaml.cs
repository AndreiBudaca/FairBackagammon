using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FairBackgammon.GameLogic;
using FairBackgammon.GameLogic.Enums;
using FairBackgammon.GameLogic.Sessions;
using FairBackgammon.GameLogic.Sessions.State;

namespace FairBackgammon.Desktop
{
  public partial class MainWindow : Window
  {
    private GameSession? _session;
    private SessionState? _lastState;

    public MainWindow()
    {
      InitializeComponent();
      UpdateUi("Ready");
    }

    private void StartNewGameButton_OnClick(object sender, RoutedEventArgs e)
    {
      try
      {
        _session = Backgammon.StartNewGame();
        _lastState = null;
        UpdateUi("New game started");
      }
      catch (Exception ex)
      {
        UpdateUi($"Error: {ex.Message}");
      }
    }

    private void RollButton_OnClick(object sender, RoutedEventArgs e)
    {
      try
      {
        EnsureSession();
        _lastState = _session!.Roll();
        UpdateUi("Rolled dice");
      }
      catch (Exception ex)
      {
        UpdateUi($"Error: {ex.Message}");
      }
    }

    private void MakeMoveButton_OnClick(object sender, RoutedEventArgs e)
    {
      try
      {
        EnsureSession();

        var move = ParseMove(MoveTextBox.Text);
        var ok = _session!.MakeMove(move);

        UpdateUi(ok ? "Move accepted" : "Move rejected (TryMakeMove returned false)");
      }
      catch (Exception ex)
      {
        UpdateUi($"Error: {ex.Message}");
      }
    }

    private void EnsureSession()
    {
      if (_session is null)
      {
        throw new InvalidOperationException("Start a new game first.");
      }
    }

    private void UpdateUi(string status)
    {
      StatusTextBlock.Text = status;

      var sb = new StringBuilder();

      sb.AppendLine("Fair Backgammon - Minimal Test UI");
      sb.AppendLine(new string('-', 40));

      if (_session is null)
      {
        sb.AppendLine("Session: <none>");
        StateTextBox.Text = sb.ToString();

        ClearBoard();
        return;
      }

      RenderBoard(_session.BoardState);

      sb.AppendLine($"Current player: {_session.CurrentPlayer}");

      if (_lastState is null)
      {
        sb.AppendLine("Last roll: <none>");
        sb.AppendLine("Valid moves: <none>");
      }
      else
      {
        sb.AppendLine($"Last roll: {_lastState.Dice.Item1}, {_lastState.Dice.Item2}");

        var validMoves = _lastState.ValidMoves?.ToArray() ?? [];
        sb.AppendLine($"Valid moves count: {validMoves.Length}");

        var preview = validMoves.ToArray();
        if (preview.Length > 0)
        {
          sb.AppendLine();
          sb.AppendLine("Valid moves:");
          foreach (var move in preview)
          {
            sb.AppendLine("  - " + FormatMove(move));
          }
        }
      }

      sb.AppendLine();
      sb.AppendLine("Move input format: from-to; from-to; ...");
      sb.AppendLine("Example: 0-5; 11-16");

      StateTextBox.Text = sb.ToString();
    }

    private void ClearBoard()
    {
      TopPointsGrid.Children.Clear();
      BottomPointsGrid.Children.Clear();
      BarPanel.Children.Clear();
      BearoffPanel.Children.Clear();
    }

    private void RenderBoard(BoardState boardState)
    {
      ClearBoard();

      var points = boardState.Points
        .OrderBy(p => p.Index)
        .ToDictionary(p => p.Index, p => p);

      // Top row: points 12..23
      for (var i = 12; i < 24; i++)
      {
        var point = points[i];
        TopPointsGrid.Children.Add(CreatePointCell(i, point.Count, point.Type));
      }

      // Bottom row: points 11..0
      for (var i = 11; i >= 0; i--)
      {
        var point = points[i];
        BottomPointsGrid.Children.Add(CreatePointCell(i, point.Count, point.Type));
      }

      foreach (var holder in boardState.Bar.OrderBy(h => h.Type))
      {
        BarPanel.Children.Add(CreateHolderCell(holder.Type.ToString(), holder.Count, holder.Type));
      }

      foreach (var holder in boardState.Off.OrderBy(h => h.Type))
      {
        BearoffPanel.Children.Add(CreateHolderCell(holder.Type.ToString(), holder.Count, holder.Type));
      }
    }

    private static UIElement CreatePointCell(int index, int count, CheckerType type)
    {
      var outer = new Border
      {
        BorderBrush = new SolidColorBrush(Color.FromArgb(0x22, 0x00, 0x00, 0x00)),
        BorderThickness = new Thickness(1),
        Margin = new Thickness(2),
        Padding = new Thickness(4)
      };

      var panel = new StackPanel();
      outer.Child = panel;

      panel.Children.Add(new TextBlock
      {
        Text = (index + 1).ToString(CultureInfo.InvariantCulture),
        FontSize = 12,
        Opacity = 0.75,
        Margin = new Thickness(0, 0, 0, 4)
      });

      panel.Children.Add(CreateCheckersStack(count, type));

      return outer;
    }

    private static UIElement CreateHolderCell(string title, int count, CheckerType type)
    {
      var outer = new Border
      {
        BorderBrush = new SolidColorBrush(Color.FromArgb(0x22, 0x00, 0x00, 0x00)),
        BorderThickness = new Thickness(1),
        Margin = new Thickness(2),
        Padding = new Thickness(4)
      };

      var panel = new StackPanel();
      outer.Child = panel;

      panel.Children.Add(new TextBlock
      {
        Text = $"{title}: {count}",
        FontSize = 12,
        Opacity = 0.85,
        Margin = new Thickness(0, 0, 0, 4)
      });

      panel.Children.Add(CreateCheckersStack(count, type));

      return outer;
    }

    private static UIElement CreateCheckersStack(int count, CheckerType type)
    {
      var stack = new StackPanel
      {
        Orientation = Orientation.Vertical
      };

      // Render up to 15 circles; if you ever exceed that, show a +N label.
      var renderCount = Math.Min(count, 15);
      for (var i = 0; i < renderCount; i++)
      {
        stack.Children.Add(new Ellipse
        {
          Width = 16,
          Height = 16,
          Margin = new Thickness(0, 0, 0, 2),
          Fill = type == CheckerType.White ? Brushes.White : Brushes.Black,
          Stroke = Brushes.DimGray,
          StrokeThickness = 1
        });
      }

      if (count > renderCount)
      {
        stack.Children.Add(new TextBlock
        {
          Text = $"+{count - renderCount}",
          FontSize = 12,
          Opacity = 0.75
        });
      }

      return stack;
    }

    private static string FormatMove((int, int)[] move)
    {
      return string.Join("; ", move.Select(p => $"{p.Item1 + 1}-{p.Item2 + 1}"));
    }

    private static (int, int)[] ParseMove(string text)
    {
      text = text.Trim();
      if (string.IsNullOrWhiteSpace(text))
      {
        throw new ArgumentException("Move text is empty.");
      }

      var parts = text
        .Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

      var moves = new List<(int, int)>();
      foreach (var part in parts)
      {
        var pair = part.Trim();
        if (pair.Length == 0) continue;

        var fromTo = pair.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (fromTo.Length != 2)
        {
          throw new FormatException($"Invalid move segment '{pair}'. Expected 'from-to'.");
        }

        var from = int.Parse(fromTo[0], CultureInfo.InvariantCulture) - 1;
        var to = int.Parse(fromTo[1], CultureInfo.InvariantCulture) - 1;
        moves.Add((from, to));
      }

      if (moves.Count == 0)
      {
        throw new FormatException("No move segments found.");
      }

      return moves.ToArray();
    }
  }
}
