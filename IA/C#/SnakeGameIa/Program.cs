namespace SnakeGameIa;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
         // Active les styles visuels pour une meilleure apparence
        Application.EnableVisualStyles();

        // Définit si les contrôles utilisent un rendu de texte compatible
        Application.SetCompatibleTextRenderingDefault(false);

        // Lance l'application avec votre formulaire principal
        Application.Run(new MainMenu());
    }    
}