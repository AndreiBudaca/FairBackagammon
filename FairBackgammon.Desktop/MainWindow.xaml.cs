using System.Globalization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
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

    private bool _isAutoPlaying;

    private readonly ObservableCollection<ValidMoveItem> _validMoves = new();

    public MainWindow()
    {
      InitializeComponent();
      ValidMovesListBox.ItemsSource = _validMoves;
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

    private async void MakeFirstMoveButton_OnClick(object sender, RoutedEventArgs e)
    {
      if (_isAutoPlaying) return;
      _isAutoPlaying = true;

      try
      {
        EnsureSession();

        while (_session is not null && _session.BoardState.Winner is null)
        {
          RollButton_OnClick(sender, e);
          await Dispatcher.Yield(DispatcherPriority.Background);

          SelectRandomValidMoveIfAny();
          await Dispatcher.Yield(DispatcherPriority.Background);

          MakeMoveButton_OnClick(sender, e);
          await Dispatcher.Yield(DispatcherPriority.Background);

          await Task.Delay(0); // Small delay to allow UI to update between moves
        }
      }
      catch (Exception ex)
      {
        UpdateUi($"Error: {ex.Message}");
      }
      finally
      {
        _isAutoPlaying = false;
      }
    }

    private void SelectRandomValidMoveIfAny()
    {
      if (_validMoves.Count == 0) return;

      var index = Random.Shared.Next(_validMoves.Count);
      for (var i = 0; i < _validMoves.Count; i++)
      {
        _validMoves[i].IsSelected = i == index;
      }
    }

    private void MakeMoveButton_OnClick(object sender, RoutedEventArgs e)
    {
      try
      {
        EnsureSession();

        var selectedMove = _validMoves.FirstOrDefault(m => m.IsSelected)?.Move;
        if (selectedMove is null)
        {
          if (_validMoves.Count == 0)
          {
            UpdateUi("No valid moves. Ending turn.");
            return; // No valid moves, nothing to do
          }

          throw new InvalidOperationException("No valid move is selected. Roll first, then select a move.");
        }

        var ok = _session!.MakeMove(selectedMove);

        UpdateUi(ok ? "Move accepted" : "Move rejected (TryMakeMove returned false)");
      }
      catch (Exception ex)
      {
        UpdateUi($"Error: {ex.Message}");
      }
    }

    private void ValidMoveCheckBox_OnChecked(object sender, RoutedEventArgs e)
    {
      if (sender is not CheckBox checkBox) return;
      if (checkBox.DataContext is not ValidMoveItem selected) return;

      foreach (var item in _validMoves)
      {
        item.IsSelected = ReferenceEquals(item, selected);
      }
    }

    private void ValidMoveCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
    {
      if (_validMoves.Count == 0) return;
      if (_validMoves.Any(m => m.IsSelected)) return;

      // Keep one selected at all times when options exist.
      _validMoves[0].IsSelected = true;
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
      var winner = _session?.BoardState.Winner;
      if (winner is null)
      {
        StatusTextBlock.Text = status;
      }
      else
      {
        var winnerName = winner.Value == (int)CheckerType.White ? "White" : "Black";
        StatusTextBlock.Text = $"Game over! Winner: {winnerName}";
      }

      var sb = new StringBuilder();

      sb.AppendLine("Fair Backgammon - Minimal Test UI");
      sb.AppendLine(new string('-', 40));

      if (_session is null)
      {
        sb.AppendLine("Session: <none>");
        StateTextBox.Text = sb.ToString();

        _validMoves.Clear();

        ClearBoard();
        return;
      }

      RenderBoard(_session.BoardState);

      if (_session.BoardState.Winner is not null)
      {
        var winnerName = _session.BoardState.Winner.Value == (int)CheckerType.White ? "White" : "Black";
        sb.AppendLine($"Winner: {winnerName}");
        sb.AppendLine();
      }

      sb.AppendLine($"Current player: {_session.CurrentPlayer}");

      if (_lastState is null)
      {
        sb.AppendLine("Last roll: <none>");
        sb.AppendLine("Valid moves: <none>");

        _validMoves.Clear();
      }
      else
      {
        sb.AppendLine($"Last roll: {_lastState.Dice.Item1}, {_lastState.Dice.Item2}");

        var validMoves = _lastState.ValidMoves?.ToArray() ?? [];
        sb.AppendLine($"Valid moves count: {validMoves.Length}");

        UpdateValidMoves(validMoves);

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
      sb.AppendLine("Move selection: pick one from the Valid moves list.");

      StateTextBox.Text = sb.ToString();
    }

    private void UpdateValidMoves((int, int)[][] validMoves)
    {
      _validMoves.Clear();

      foreach (var move in validMoves)
      {
        _validMoves.Add(new ValidMoveItem(FormatMove(move), move));
      }

      if (_validMoves.Count > 0)
      {
        _validMoves[0].IsSelected = true;
      }
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

      // Top row: points 13..25
      for (var i = 13; i < 25; i++)
      {
        var point = points[i];
        TopPointsGrid.Children.Add(CreatePointCell(i, point.Count, point.Type));
      }

      // Bottom row: points 12..1
      for (var i = 12; i >= 1; i--)
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
        Text = index.ToString(CultureInfo.InvariantCulture),
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
      return string.Join("; ", move.Select(p => $"{p.Item1}-{p.Item2}"));
    }

    private sealed class ValidMoveItem : INotifyPropertyChanged
    {
      private bool _isSelected;

      public ValidMoveItem(string displayText, (int, int)[] move)
      {
        DisplayText = displayText;
        Move = move;
      }

      public string DisplayText { get; }
      public (int, int)[] Move { get; }

      public bool IsSelected
      {
        get => _isSelected;
        set
        {
          if (value == _isSelected) return;
          _isSelected = value;
          OnPropertyChanged();
        }
      }

      public event PropertyChangedEventHandler? PropertyChanged;

      private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
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

        var from = int.Parse(fromTo[0], CultureInfo.InvariantCulture);
        var to = int.Parse(fromTo[1], CultureInfo.InvariantCulture);
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
