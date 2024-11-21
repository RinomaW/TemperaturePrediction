using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;

public class AI
{
    private int gridSize;
    private double[,] qTable; // Table des Q-values
    private Random rand;
    private double learningRate = 0.5; // Taux d'apprentissage
    private double discountFactor = 0.25; // Facteur de discount
    private double explorationRate = 0.25; // Taux d'exploration (epsilon-greedy)
    private string filePath; // Chemin pour sauvegarder/charger la Q-table

    public AI(int gridSize, string filePath)
{
    this.gridSize = gridSize;
    this.filePath = filePath;
    rand = new Random();
    qTable = new double[gridSize, gridSize]; // Explicitly initialize with zeros
    LoadQTable();
}
    // Obtenir l'action suivante (avec stratégie epsilon-greedy)
    public Point GetNextMove(List<Point> snake, Point food)
    {
        var currentState = GetState(snake); // Récupérer l'état actuel
        if (rand.NextDouble() < explorationRate)
        {
            return GetRandomAction(); // Exploration : choisir une action aléatoire
        }
        else
        {
            return GetBestAction(currentState); // Exploitation : choisir la meilleure action
        }
    }

    // Obtenir l'état actuel du serpent
    private Point GetState(List<Point> snake)
    {
        // L'état peut être défini par la position de la tête du serpent
        return snake[0];
    }

    // Choisir une action aléatoire (exploration)
    private Point GetRandomAction()
    {
        var directions = new[] { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) };
        return directions[rand.Next(directions.Length)];
    }

    // Choisir la meilleure action (exploitation) en utilisant la Q-table
    private Point GetBestAction(Point state)
    {
        var directions = new[] { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) };
        var bestAction = directions[0];
        double bestValue = double.MinValue;

        // Rechercher la meilleure action en fonction de la Q-table
        foreach (var dir in directions)
        {
            var newState = new Point(state.X + dir.X, state.Y + dir.Y);
            if (IsWithinGrid(newState))
            {
                double qValue = qTable[newState.X, newState.Y];
                if (qValue > bestValue)
                {
                    bestValue = qValue;
                    bestAction = dir;
                }
            }
        }
        
        return bestAction;
    }

    // Mettre à jour la Q-table en fonction de l'expérience
    public void UpdateQTable(Point currentState, Point action, double reward, Point nextState)
    {
        double oldQValue = qTable[currentState.X, currentState.Y];
        double maxNextQValue = GetMaxQValue(nextState);
        // Mise à jour de la Q-value avec la formule de Q-learning
        qTable[currentState.X, currentState.Y] = oldQValue + learningRate * (reward + discountFactor * maxNextQValue - oldQValue);
       
    }

    // Obtenir la Q-value maximale pour un état donné (la meilleure action possible)
    private double GetMaxQValue(Point state)
    {
        var directions = new[] { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) };
        double maxQValue = double.MinValue;
        foreach (var dir in directions)
        {
            var newState = new Point(state.X + dir.X, state.Y + dir.Y);
            if (IsWithinGrid(newState))
            {
                double qValue = qTable[newState.X, newState.Y];
                if (qValue > maxQValue)
                {
                    maxQValue = qValue;
                }
            }
        }
        return maxQValue;
    }

    // Vérifier si un point est dans les limites de la grille
    private bool IsWithinGrid(Point point)
    {
        return point.X >= 0 && point.X < gridSize && point.Y >= 0 && point.Y < gridSize;
    }

    // Sauvegarder la Q-table dans un fichier
    public void SaveQTable()
    {
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true  // Rendre le JSON plus lisible
        };
        // Convertir le tableau 2D en tableau de tableaux
        double[][] serializableQTable = ConvertToJaggedArray(qTable);
        string json = JsonSerializer.Serialize(serializableQTable, options);
        File.WriteAllText(filePath, json);
    }

    // Méthode utilitaire pour convertir un tableau 2D en tableau de tableaux
    private double[][] ConvertToJaggedArray(double[,] array)
    {
        int rows = array.GetLength(0);
        int cols = array.GetLength(1);
        var jaggedArray = new double[rows][];
        for (int i = 0; i < rows; i++)
        {
            jaggedArray[i] = new double[cols];
            for (int j = 0; j < cols; j++)
            {
                jaggedArray[i][j] = array[i, j];
            }
        }
        return jaggedArray;
    }


  private void LoadQTable()
    {
        if (File.Exists(filePath))
        {
            try 
            {
                string json = File.ReadAllText(filePath);
                if (!string.IsNullOrEmpty(json))
                {
                    // Désérialiser en tableau de tableaux
                    double[][] jaggedArray = JsonSerializer.Deserialize<double[][]>(json);
                    qTable = ConvertTo2DArray(jaggedArray);
                }
                else
                {
                    qTable = new double[gridSize, gridSize];
                }
            }
            catch
            {
                qTable = new double[gridSize, gridSize];
            }
        }
        else
        {
            qTable = new double[gridSize, gridSize];
        }
    }

    // Méthode utilitaire pour convertir un tableau de tableaux en tableau 2D
    private double[,] ConvertTo2DArray(double[][] jaggedArray)
    {
        int rows = jaggedArray.Length;
        int cols = jaggedArray[0].Length;
        var array = new double[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                array[i, j] = jaggedArray[i][j];
            }
        }
        return array;
    }

}
