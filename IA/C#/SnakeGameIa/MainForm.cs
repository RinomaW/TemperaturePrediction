public class MainMenu : Form
{
    public MainMenu()
    {
        this.Text = "Mon Application";
        this.Width = 800;
        this.Height = 600;

        var label = new Label
        {
            Text = "Bienvenue dans mon application Windows Forms !",
            Dock = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        };

        var button = new Button
        {
            Text = "Jouer au Snake",
            Dock = DockStyle.Top,
            Height = 50
        };
        button.Click += (s, e) => OpenSnakeGame();

        this.Controls.Add(button);
        this.Controls.Add(label);
    }

    private void OpenSnakeGame()
    {
      var snakeGames = new List<SnakeGame>();

for (int i = 0; i < 20; i++)
{
    var snakeGame = new SnakeGame();
    snakeGames.Add(snakeGame);
    snakeGame.Show();
}

    }
}
