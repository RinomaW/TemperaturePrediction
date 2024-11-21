public class GameLogic
{
    public List<Point> Snake { get; private set; }
    public Point Food { get; private set; }
     private int gridSize;
public bool GameOver { get; set; }
    public int GridSize
    {
        get { return gridSize; }
    }
    private Random rand;

    public GameLogic(int gridSize)
    {
        this.gridSize = gridSize;
        rand = new Random();
        Snake = new List<Point> { new Point(gridSize / 2, gridSize / 2) }; // Le serpent commence au centre
        SpawnFood();
    }

  public void MoveSnake(Direction direction)
{
    Point newHead;

    // Détermine la nouvelle tête en fonction de la direction
    switch (direction)
    {
        case Direction.Up:
            newHead = new Point(Snake[0].X, Snake[0].Y - 1);
            break;
        case Direction.Down:
            newHead = new Point(Snake[0].X, Snake[0].Y + 1);
            break;
        case Direction.Left:
            newHead = new Point(Snake[0].X - 1, Snake[0].Y);
            break;
        case Direction.Right:
            newHead = new Point(Snake[0].X + 1, Snake[0].Y);
            break;
        default:
            newHead = Snake[0];
            break;
    }

    // Vérifie si le serpent dépasse les bords
    if (!IsInsideBounds(newHead))
    {
        // Repositionne le serpent au centre ou à une position aléatoire
        newHead = new Point(gridSize / 2, gridSize / 2);
        // Vous pourriez aussi utiliser une position aléatoire à l'intérieur des limites
        // newHead = new Point(rand.Next(gridSize), rand.Next(gridSize));

        // Appliquez une pénalité à l'IA
        // ai.UpdateQTable(Snake[0], direction.ToPoint(), -10, newHead); // Exemple de pénalité
    }

    // Le serpent avance en ajoutant la nouvelle tête au début de la liste
    Snake.Insert(0, newHead);

    // Vérifie si le serpent se mord
    if (Snake.Skip(1).Contains(newHead))
    {
        throw new InvalidOperationException("Game Over");
    }

    // Enlève la queue du serpent
    Snake.RemoveAt(Snake.Count - 1);
}


    public void GrowSnake()
    {
        // Ajouter un segment à la fin du serpent
        Snake.Add(new Point(-1, -1));
    }

    public void Draw(Graphics g)
    {
        // Dessine le serpent
        foreach (var segment in Snake)
        {
            g.FillRectangle(Brushes.Green, segment.X * 20, segment.Y * 20, 20, 20);
        }

        // Dessine la nourriture
        g.FillRectangle(Brushes.Red, Food.X * 20, Food.Y * 20, 20, 20);
    }

    private bool IsInsideBounds(Point point)
    {
        return point.X > 0 && point.X +1 < gridSize && point.Y > 0 && point.Y +1 < gridSize;
    }

    public void SpawnFood()
    {
        // Générer une nouvelle position de nourriture aléatoire
var x = rand.Next(1, gridSize - 2); // Plage valide : évite 0 et gridSize
var y = rand.Next(1, gridSize - 2); // Plage valide : évite 0 et gridSize

// Définir la position de la nourriture
Food = new Point(x, y);

    }
  public void ResetGame()
{
    GameOver = false;
    Snake = new List<Point> { new Point(gridSize / 2, gridSize / 2) };  // Initialize Snake list properly
    SpawnFood();  // Réinitialiser la nourriture
    
}

     
    public bool IsGameOver()
{
    Point head = Snake[0];

    // Vérification si le serpent se mord ou sort des limites
    // Se mord : la tête du serpent est dans le corps
    if (Snake.Skip(1).Contains(head))
    {
        return true;
    }

    // Sort des limites du jeu : vérifier si la tête dépasse la grille
    if (head.X < 0 || head.Y < 0 || head.X >= GridSize || head.Y >= GridSize)
    {
        return true;
    }

    return false;
}

}

