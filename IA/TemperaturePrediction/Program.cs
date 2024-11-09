using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;

public class TemperatureData
{
    [LoadColumn(0)] // Date
    public DateTime Date { get; set; }

    [LoadColumn(1)] // Room number
    public float RoomNumber { get; set; }

    [LoadColumn(2)] // fenetre_ouverte
    public float FenetreOuverte { get; set; }

    [LoadColumn(3)] // temp_ext
    public float TempExt { get; set; }

    [LoadColumn(4)] // nb_personnes
    public float NbPersonnes { get; set; }

    [LoadColumn(5)] // humidite
    public float Humidite { get; set; }

    [LoadColumn(6)] // temp_int
    public float TempInt { get; set; }
}

public class TemperaturePrediction
{
    [ColumnName("Score")]
    public float PredictedTempInt { get; set; }
}

public class Program
{  static void Main(string[] args)
    {
        string dataPath = "./donnees_temperature.csv";
        var context = new MLContext();

        if (!File.Exists(dataPath))
        {
            Console.WriteLine("File not found: " + dataPath);
            return;
        }

        // Charger les données à partir du fichier CSV
        var data = context.Data.LoadFromTextFile<TemperatureData>(dataPath, separatorChar: ',', hasHeader: true);
        var trainTestData = context.Data.TrainTestSplit(data, testFraction: 0.2);
        var trainingData = trainTestData.TrainSet;
        var testData = trainTestData.TestSet;

        var pipeline = context.Transforms.Concatenate("Features", "RoomNumber", "FenetreOuverte", "TempExt", "NbPersonnes", "Humidite")
            .Append(context.Regression.Trainers.Sdca(labelColumnName: "TempInt", maximumNumberOfIterations: 100));

        var model = pipeline.Fit(trainingData);

        var predictions = model.Transform(testData);
        var metrics = context.Regression.Evaluate(predictions, labelColumnName: "TempInt");
        Console.WriteLine($"Mean Absolute Error (MAE): {metrics.MeanAbsoluteError}");
        Console.WriteLine($"Mean Squared Error (MSE): {metrics.MeanSquaredError}");

        var random = new Random();
        var uniqueRooms = GetUniqueRooms(dataPath); // Liste des numéros de salle
        var predictionEngine = context.Model.CreatePredictionEngine<TemperatureData, TemperaturePrediction>(model);

        foreach (var room in uniqueRooms)
        {
            var lastObservation = GetLastObservationByRoom(dataPath, room);

            var roomPredictions = new Dictionary<string, float>();

            // Récupérer l'heure actuelle et arrondir à l'heure entière suivante
            var currentHour = DateTime.Now;
            if (currentHour.Minute > 0)
            {
                currentHour = currentHour.AddHours(1).AddMinutes(-currentHour.Minute); // Arrondir à l'heure suivante
            }

            // Prédictions pour chaque heure à partir de l'heure actuelle jusqu'à 17h après l'heure actuelle
            for (int hour = currentHour.Hour; hour < currentHour.Hour + 17; hour++) // De l'heure actuelle à 17h après l'heure actuelle
            {
                var futureDate = currentHour.Date.AddHours(hour); // Construire une date avec l'heure ronde

                // Simuler les conditions futures basées sur la dernière observation
                var humiditeFuture = lastObservation.Item2.Humidite * (1 + (float)(random.NextDouble() * 0.1 - 0.05)); // +/- 5% change
                var tempExtFuture = lastObservation.Item2.TempExt + (float)(random.NextDouble() * 2 - 1); // +/- 1°C change
                var fenetreOuverteFuture = lastObservation.Item2.FenetreOuverte;

                // Vérifier si NbPersonnes est zéro; sinon, l'exclure des caractéristiques
                var features = new List<float> { lastObservation.Item2.RoomNumber, fenetreOuverteFuture, tempExtFuture, humiditeFuture };
                if (lastObservation.Item2.NbPersonnes > 0)
                {
                    features.Insert(3, lastObservation.Item2.NbPersonnes); // Ajouter NbPersonnes si non nul
                }

                var futureData = new TemperatureData
                {
                    RoomNumber = room,
                    FenetreOuverte = fenetreOuverteFuture,
                    TempExt = tempExtFuture,
                    NbPersonnes = lastObservation.Item2.NbPersonnes,
                    Humidite = humiditeFuture
                };

                // Faire la prédiction
                var prediction = predictionEngine.Predict(futureData).PredictedTempInt;

                // Formater l'affichage basé sur l'heure future
                string formattedDate = futureDate.ToString("dddd HH:mm", CultureInfo.InvariantCulture);
                roomPredictions[formattedDate] = prediction;
            }

                Console.WriteLine($"Salle {room} :");
                foreach (var prediction in roomPredictions)
                {
                    Console.WriteLine($"{prediction.Key}: {(int)prediction.Value}°C");
                }
                Console.WriteLine();
            
            DateTime currentDay = DateTime.Now; // La date actuelle
            
            roomPredictions = new Dictionary<string, float>();


    // Calcul des prédictions pour les 3 prochains jours
    for (int dayOffset = 1; dayOffset <= 3; dayOffset++) 
    {
        DateTime futureDay = currentDay.AddDays(dayOffset);
        string dayName = futureDay.ToString("dddd", CultureInfo.InvariantCulture); // Exemple : "Samedi", "Dimanche", ...

        // Simuler les conditions futures basées sur la dernière observation
        var humiditeFuture = lastObservation.Item2.Humidite * (1 + (float)(random.NextDouble() * 0.1 - 0.05)); // +/- 5% de variation
        var tempExtFuture = lastObservation.Item2.TempExt + (float)(random.NextDouble() * 2 - 1); // +/- 1°C de variation
        var fenetreOuverteFuture = lastObservation.Item2.FenetreOuverte;

        var features = new List<float> { lastObservation.Item2.RoomNumber, fenetreOuverteFuture, tempExtFuture, humiditeFuture };
        if (lastObservation.Item2.NbPersonnes > 0)
        {
            features.Insert(3, lastObservation.Item2.NbPersonnes); // Ajouter NbPersonnes si non nul
        }

        var futureData = new TemperatureData
        {
            RoomNumber = room,
            FenetreOuverte = fenetreOuverteFuture,
            TempExt = tempExtFuture,
            NbPersonnes = lastObservation.Item2.NbPersonnes,
            Humidite = humiditeFuture
        };

        // Faire la prédiction
        var prediction = predictionEngine.Predict(futureData).PredictedTempInt;

        // Ajouter la prédiction à la liste des prévisions pour ce jour
        roomPredictions[dayName] = prediction;
    }


                           Console.WriteLine();
                foreach (var prediction in roomPredictions)
                {
                    Console.WriteLine($"{prediction.Key}: {(int)prediction.Value}°C");
                }
                Console.WriteLine("\n");
        }
    }

   
private static (DateTime, TemperatureData) GetLastObservationByRoom(string dataPath, int roomNumber)
{
    var lines = File.ReadAllLines(dataPath)
                    .Where(line => line.Split(',')[1] == roomNumber.ToString())
                    .ToArray();
    if (lines.Length == 0)
    {
        throw new InvalidDataException($"No data found for room number {roomNumber}.");
    }

    var lastLine = lines[lines.Length - 1].Split(',');
    var date = DateTime.ParseExact(lastLine[0], "yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture);

    TemperatureData observation;
    if (float.Parse(lastLine[4]) > 0)
    {
        observation = new TemperatureData
        {
            Date = date,
            RoomNumber = roomNumber,
            FenetreOuverte = float.Parse(lastLine[2]),
            TempExt = float.Parse(lastLine[3]),
            NbPersonnes = float.Parse(lastLine[4]),
            Humidite = float.Parse(lastLine[5]),
            TempInt = float.Parse(lastLine[6])
        };
    }
    else
    {
        observation = new TemperatureData
        {
            Date = date,
            RoomNumber = roomNumber,
            FenetreOuverte = float.Parse(lastLine[2]),
            TempExt = float.Parse(lastLine[3]),
            Humidite = float.Parse(lastLine[5]),
            TempInt = float.Parse(lastLine[6])
        };
    }

    return (date, observation);
}

   private static List<int> GetUniqueRooms(string dataPath)
{
    return File.ReadAllLines(dataPath)
               .Skip(1)
               .Select(line => int.Parse(line.Split(',')[1]))  // Assuming the room number is in the second column
               .Distinct()
               .ToList();
}

}
