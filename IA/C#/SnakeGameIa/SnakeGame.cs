using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

public class SnakeGame : Form
{
    private GameLogic gameLogic;
    private bool gameOver;
    private Direction currentDirection;
    private AI ai; // L'instance de l'IA
    private List<Point> snake; // Référence à la liste de points pour le serpent
    private readonly string aiFilePath = "qTable.json"; // Chemin du fichier JSON pour sauvegarder la Q-table

    public SnakeGame()
    {
        this.Text = "Snake Game";
        this.ClientSize = new Size(380, 380);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterScreen;

        gameLogic = new GameLogic(20); // Taille de la grille
        currentDirection = Direction.Right; // Le serpent commence en allant à droite
        ai = new AI(20, aiFilePath); // Créer une instance de l'IA avec le fichier JSON
        snake = gameLogic.Snake; // Initialiser la référence du serpent

        // Utiliser System.Windows.Forms.Timer explicitement
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 1 };
        timer.Tick += (s, e) => GameLoop();
        timer.Start();

        this.Paint += (s, e) => DrawGame(e.Graphics);
        this.KeyDown += OnKeyDown; // Gestion des appuis sur les touches
        this.KeyPreview = true; // Permet à la fenêtre de capturer les événements clavier même si le focus est sur un autre contrôle
    }

    private void GameLoop()
    {
        if (gameOver)
        {
            ai.SaveQTable();
             // Sauvegarder la Q-table lorsque le jeu est terminé
            gameOver = false;
            gameLogic.ResetGame();
            currentDirection = Direction.Right; // Reset direction
        }
        
        // Utiliser l'IA pour déterminer la direction suivante
        currentDirection = GetDirectionFromAI();

        try
        {
            // Déplacer le serpent
            gameLogic.MoveSnake(currentDirection);

            // Vérifier si le serpent a mangé la nourriture
            if (gameLogic.Snake[0] == gameLogic.Food)
            {
                gameLogic.GrowSnake();
                gameLogic.SpawnFood();
            }

            // Mettre à jour la Q-table de l'IA
            ai.UpdateQTable(gameLogic.Snake[0], currentDirection.ToPoint(), CalculateReward(), gameLogic.Snake[0]);
            
            // Vérifier si le jeu est terminé
            if (gameLogic.IsGameOver())
            {
                gameOver = true;
            }
        }
        catch (InvalidOperationException)
        {
            gameOver = true; // Gérer le scénario de fin de jeu
        }

        Invalidate(); // Redessiner le jeu
    }

    private Direction GetDirectionFromAI()
    {
        // Utiliser l'IA pour obtenir la meilleure direction
        Point nextMove = ai.GetNextMove(gameLogic.Snake, gameLogic.Food);

        // Convertir le Point en Direction avec ToPoint
        if (nextMove == Direction.Up.ToPoint()) return Direction.Up;
        if (nextMove == Direction.Down.ToPoint()) return Direction.Down;
        if (nextMove == Direction.Left.ToPoint()) return Direction.Left;
        if (nextMove == Direction.Right.ToPoint()) return Direction.Right;

        return currentDirection; // Retourner la direction actuelle si l'IA échoue à choisir une direction valide
    }

    private double CalculateReward()
    {
        Point snakeHead = gameLogic.Snake[0];
        Point food = gameLogic.Food;

        double distanceBeforeMove = GetDistance(snakeHead, food);
        Point nextHead = new Point(snakeHead.X + currentDirection.ToPoint().X, snakeHead.Y + currentDirection.ToPoint().Y);
        double distanceAfterMove = GetDistance(nextHead, food);

        if (snakeHead == food)
        {
            return 10000; // Récompense pour avoir mangé
            ai.SaveQTable();
        }
        else if (gameLogic.IsGameOver())
        {
            return -20; // Récompense négative pour la fin de jeu
             ai.SaveQTable();
        }
        else if (distanceAfterMove < distanceBeforeMove)

        {
            return 1; // Récompense pour s'approcher de la nourriture
             ai.SaveQTable();
        }
        else
        {
            return -3; // Pénalité pour s'éloigner
             ai.SaveQTable();
        }
    }

    private double GetDistance(Point p1, Point p2)
    {
        return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
    }

    private void DrawGame(Graphics g)
    {
        g.Clear(Color.Gray); // Fond gris
        gameLogic.Draw(g);
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (gameOver) return;

        switch (e.KeyCode)
        {
            case Keys.Up:
                if (currentDirection != Direction.Down) currentDirection = Direction.Up;
                break;
            case Keys.Down:
                if (currentDirection != Direction.Up) currentDirection = Direction.Down;
                break;
            case Keys.Left:
                if (currentDirection != Direction.Right) currentDirection = Direction.Left;
                break;
            case Keys.Right:
                if (currentDirection != Direction.Left) currentDirection = Direction.Right;
                break;
        }
    }
}
