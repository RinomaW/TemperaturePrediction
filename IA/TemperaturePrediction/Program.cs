using System;

public class Program
{
    static void Main(string[] args)
    {
        //string dataPath = "./donnees_temperature.csv";
        string dataPath = "C:\\Users\\romwe\\Documents\\My Web Sites\\SAE5\\IA\\TemperaturePrediction\\donnees_temperature.csv";
        try
        {
            var model = new TemperaturePredictionModel(dataPath);
            model.TrainModel();

            var uniqueRooms = TemperaturePredictionModel.GetUniqueRooms(dataPath); // Get list of unique room numbers
            foreach (var room in uniqueRooms)
            {
                model.PredictTemperatureForRoom((int)room); // Cast float to int if needed
            }

            Console.WriteLine("coucou, test de model fenetre si open ou non --------------------------*/*-----------------");

             var modelFenetre = new FenetreBoolOuverte(dataPath);

            // Train the model
            modelFenetre.TrainModel();

            // Get unique rooms
            var uniqueRoomsFenetre = TemperaturePredictionModel.GetUniqueRooms(dataPath);
              foreach (var room in uniqueRoomsFenetre)
                {
                    // Predict window state over time for each room
                    bool isWindowOpen = modelFenetre.PredictIfWindowIsOpenOverTime(
                        roomNumber: room,
                        startTime: DateTime.Now,
                        intervalMinutes: 30, // Example: 30 minutes
                        initialTempInt: 22.5f,
                        dataPathCSV: dataPath
                    );

                // Only display if the state has changed (i.e., the window is open or closed)
                if (isWindowOpen)
                    {
                        Console.WriteLine($"Room {room}: Window open? True");
                    }
                    else
                    {
                        Console.WriteLine($"Room {room}: Window open? False");
                    }
                }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
