using AutoTube.AI.Console.Controllers;
using AutoTube.AI.Console.Services;

namespace AutoTube.AI.Console
{
    public static class Program
    {
        public async static Task Main()
        {
            CheckDirectories();
            PrintAutoTube();

            do
            {
                var content = PrintContentSelection();
                await MainController.StartProcess(content);

            } while (true) ;
        }

        private static void CheckDirectories()
        {
            if (Directory.Exists(CommonService.TempPath))
            {
                Directory.Delete(CommonService.TempPath, true);
            }

            Directory.CreateDirectory(CommonService.OutputPath);
            Directory.CreateDirectory(CommonService.TempPath);
        }

        private static string? PrintContentSelection()
        {
            System.Console.WriteLine("¿Quieres generar contenido sobre algún personaje concreto?");
            System.Console.WriteLine("- Indica el nombre del personaje o pulsa 'INTRO' para un persona aleatorio");
            System.Console.WriteLine("- Escribe 'EXIT' para cerrar el programa");

            System.Console.WriteLine();
            System.Console.Write("Elige una opción: ");

            var option = System.Console.ReadLine();
            if (string.Compare(option, "EXIT", StringComparison.OrdinalIgnoreCase) == 0)
            {
                Environment.Exit(0);
            }

            return option;
        }

        private static void PrintAutoTube()
        {
            var header = @"
      ___           ___                         ___                         ___                         ___     
     /\  \         /\  \                       /\  \                       /\  \         _____         /\__\    
    /::\  \        \:\  \         ___         /::\  \         ___          \:\  \       /::\  \       /:/ _/_   
   /:/\:\  \        \:\  \       /\__\       /:/\:\  \       /\__\          \:\  \     /:/\:\  \     /:/ /\__\  
  /:/ /::\  \   ___  \:\  \     /:/  /      /:/  \:\  \     /:/  /      ___  \:\  \   /:/ /::\__\   /:/ /:/ _/_ 
 /:/_/:/\:\__\ /\  \  \:\__\   /:/__/      /:/__/ \:\__\   /:/__/      /\  \  \:\__\ /:/_/:/\:|__| /:/_/:/ /\__\
 \:\/:/  \/__/ \:\  \ /:/  /  /::\  \      \:\  \ /:/  /  /::\  \      \:\  \ /:/  / \:\/:/ /:/  / \:\/:/ /:/  /
  \::/__/       \:\  /:/  /  /:/\:\  \      \:\  /:/  /  /:/\:\  \      \:\  /:/  /   \::/_/:/  /   \::/_/:/  / 
   \:\  \        \:\/:/  /   \/__\:\  \      \:\/:/  /   \/__\:\  \      \:\/:/  /     \:\/:/  /     \:\/:/  /  
    \:\__\        \::/  /         \:\__\      \::/  /         \:\__\      \::/  /       \::/  /       \::/  /   
     \/__/         \/__/           \/__/       \/__/           \/__/       \/__/         \/__/         \/__/    

                                                                                            { by Johnny Romero; }
";
            System.Console.WriteLine(header);
        }
    }
}
