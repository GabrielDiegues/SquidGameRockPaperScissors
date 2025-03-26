using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

// Enum representing Rock-Paper-Scissors choices
public enum RockPaperScissors
{
    Rock,     // 0
    Paper,    // 1
    Scissors  // 2
}

class Program
{
    // Dictionary to store player statistics (wins, draws, losses)
    static Dictionary<string, (int vitórias, int empates, int derrotas)> jogadores = new Dictionary<string, (int, int, int)>();

    // Game configuration
    static int countdownTime = 6;  // Time limit for player input
    static readonly string PC = "pc";
    static readonly string PLAYER = "player";

    // Thread-safe random number generator
    static readonly Random myRandom = new Random();
    private static readonly object _lock = new object();

    /// <summary>
    /// Generates a random number in a thread-safe manner
    /// </summary>
    /// <param name="num">Upper bound (exclusive)</param>
    /// <returns>Random integer between 0 and num-1</returns>
    static int GetRandomNumber(int num)
    {
        lock (_lock)
        {
            return myRandom.Next(num);
        }
    }


    // Possible computer play combinations (Rock=0, Paper=1, Scissors=2)
    static List<List<char>> possibleComputerPlays = new List<List<char>>
    {
        new List<char> { '0', '1' },  // Rock + Paper
        new List<char> { '0', '2' },  // Rock + Scissors
        new List<char> { '1', '2' }   // Paper + Scissors
    };

    /// <summary>
    /// Main game loop
    /// </summary>
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Welcome message
        Console.WriteLine("😀 Olá! Vamos jogar Jokempo?");
        Console.WriteLine("1 - Sim ou 0 - Não");

        var continuar = ValidarEntrada('0', '1');

        while (continuar != '0')
        {
            // Player registration
            string nomeJogador = ObterNomeJogador();

            if (!jogadores.ContainsKey(nomeJogador))
            {
                jogadores[nomeJogador] = (0, 0, 0);  // Initialize stats
            }

            Console.WriteLine($"Bem-vindo, {nomeJogador}! Vamos começar...");

            do
            {
                // Adjust difficulty by reducing time
                if (countdownTime != 2)
                {
                    countdownTime--;
                }

                // Get player and computer choices
                List<char> opcao = ObterOpcaoJogador();
                int randomIndex = GetRandomNumber(possibleComputerPlays.Count);
                List<char> opcaoPC = possibleComputerPlays[randomIndex];

                // Play round and determine winner
                int matchWinner = await JogarRodada(opcao, opcaoPC);
                if (matchWinner == 1)
                {
                    Console.WriteLine("\nComputer Won");
                }
                else if (matchWinner == -1)
                {
                    Console.WriteLine("\nPlayer won");
                }
                else
                {
                    Console.WriteLine("\nDraw");
                }

                // Update statistics
                AtualizarEstatisticas(nomeJogador, matchWinner, opcao, opcaoPC);

                // Play again prompt
                Console.WriteLine("\nQuer jogar de novo?");
                Console.WriteLine("1 - Sim, 0 - Não");
            } while (Console.ReadKey().KeyChar == '1');

            countdownTime = 6;  // Reset timer

            // Post-game menu
            Console.WriteLine("\nO que deseja fazer agora?");
            Console.WriteLine("1 - Continuar com outro jogador, 2 - Listar jogadores e estatísticas, 0 - Sair");
            continuar = ValidarEntrada('0', '1', '2');

            if (continuar == '2')
            {
                ListarEstatisticasJogadores(ref continuar);
            }
        }

        Console.WriteLine("👋 Tchau! Até a próxima");
    }

    /// <summary>
    /// Validates user input against allowed options
    /// </summary>
    static char ValidarEntrada(params char[] opcoesValidas)
    {
        char opcao = Console.ReadKey().KeyChar;
        while (Array.IndexOf(opcoesValidas, opcao) == -1)
        {
            Console.WriteLine("\nOpção inválida. Tente novamente.");
            opcao = Console.ReadKey().KeyChar;
        }
        return opcao;
    }

    /// <summary>
    /// Gets and validates player name
    /// </summary>
    static string ObterNomeJogador()
    {
        Console.WriteLine("\nQual é o seu nome?");
        string nomeJogador = Console.ReadLine();

        while (string.IsNullOrEmpty(nomeJogador))
        {
            Console.WriteLine("\nVocê precisa digitar o seu nome. Pode ser o seu apelido...");
            nomeJogador = Console.ReadLine();
        }

        return nomeJogador;
    }

    /// <summary>
    /// Gets player's two choices (Rock/Paper/Scissors)
    /// </summary>
    static List<char> ObterOpcaoJogador()
    {
        List<char> playerHand = new List<char>([]);
        for (int i = 0; i < 2; i++)
        {
            playerHand.Add(getPlayerRockPaperOrScissors());
        }
        return playerHand;
    }

    /// <summary>
    /// Plays a game round between player and computer
    /// </summary>
    static async Task<int> JogarRodada(List<char> opcao, List<char> opcaoPC)
    {
        // Computer decision logic
        int pcsPlayChoice = 0;
        opcaoPC.Sort();

        if (opcao.OrderBy(x => x).SequenceEqual(opcaoPC))
        {
            pcsPlayChoice = opcaoPC[1] - '0';
        }
        else
        {
            pcsPlayChoice = minimax(opcao, opcaoPC);
        }

        // Display choices
        RockPaperScissors[] allPlayOptions = (RockPaperScissors[])Enum.GetValues(typeof(RockPaperScissors));
        Console.WriteLine($"\nYour hand: {allPlayOptions[opcao[0] - '0']} | {allPlayOptions[opcao[1] - '0']}");
        Console.WriteLine($"\nComputer's hand: {allPlayOptions[opcaoPC[0] - '0']} | {allPlayOptions[opcaoPC[1] - '0']}");

        // Get player input with timeout
        char userInput = await GetUserInputWithTimeoutAsync(countdownTime, opcao);

        // Handle timeout or invalid input
        if (userInput == '\0')
        {
            Console.WriteLine("\nTime's up! No input received. Choosing a random option");
            int randomIndex = GetRandomNumber(opcao.Count);
            userInput = opcao[randomIndex];
        }
        else if (userInput != opcao[0] && userInput != opcao[1])
        {
            Console.WriteLine("\nYou typed a wrong value. Choosing a random option");
            int randomIndex = GetRandomNumber(opcao.Count);
            userInput = opcao[randomIndex];
        }
        else
        {
            Console.WriteLine($"\nYou typed: {userInput}");
        }

        // Determine winner
        int playersPlayChoice = userInput - '0';
        Console.WriteLine($"\nYou chose: {allPlayOptions[playersPlayChoice]}");
        Console.WriteLine($"\nComputer chose: {allPlayOptions[pcsPlayChoice]}");
        int matchWinner = utility(playersPlayChoice, pcsPlayChoice);
        return matchWinner;
    }

    /// <summary>
    /// Updates player statistics based on match result
    /// </summary>
    static void AtualizarEstatisticas(string nomeJogador, int vitoria, List<char> opcao, List<char> opcaoPC)
    {
        switch (vitoria)
        {
            case 1:  // Computer won
                jogadores[nomeJogador] = (jogadores[nomeJogador].vitórias, jogadores[nomeJogador].empates, jogadores[nomeJogador].derrotas + 1);
                break;
            case 0:  // Draw
                jogadores[nomeJogador] = (jogadores[nomeJogador].vitórias, jogadores[nomeJogador].empates + 1, jogadores[nomeJogador].derrotas);
                break;
            case -1: // Player won
                jogadores[nomeJogador] = (jogadores[nomeJogador].vitórias + 1, jogadores[nomeJogador].empates, jogadores[nomeJogador].derrotas);
                break;
        }
    }

    /// <summary>
    /// Displays all players' statistics
    /// </summary>
    static void ListarEstatisticasJogadores(ref char continuar)
    {
        Console.WriteLine("\nJogadores e suas estatísticas:\n");
        Console.WriteLine("===================================================================");
        foreach (var jogador in jogadores)
        {
            Console.WriteLine($"{jogador.Key}: {jogador.Value.vitórias} vitórias, {jogador.Value.empates} empates, {jogador.Value.derrotas} derrotas");
        }
        Console.WriteLine("===================================================================\n");

        Console.WriteLine("E agora? Quer iniciar uma nova partida?");
        Console.WriteLine("1 - Sim ou 0 - Não");

        continuar = ValidarEntrada('0', '1');
    }

    /// <summary>
    /// Gets user input with timeout
    /// </summary>
    static async Task<char> GetUserInputWithTimeoutAsync(int timeoutSeconds, List<char> playersHand)
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        Console.WriteLine($"\nYou have {timeoutSeconds} seconds to press a key...");

        try
        {
            var inputTask = Task.Run(() =>
            {
                Console.Write($"\nType {playersHand[0]} or {playersHand[1]}");
                return Console.ReadKey(intercept: true).KeyChar;
            }, cts.Token);

            var timeoutTask = Task.Delay(Timeout.Infinite, cts.Token);
            var completedTask = await Task.WhenAny(inputTask, timeoutTask);

            if (completedTask == inputTask)
            {
                cts.Cancel();
                return await inputTask;
            }
            else
            {
                Console.WriteLine("\nTime's up!");
                return '\0';
            }
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("\nTime's up!");
            return '\0';
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
            return '\0';
        }
    }

    /// <summary>
    /// Gets a single Rock/Paper/Scissors choice from player
    /// </summary>
    static char getPlayerRockPaperOrScissors()
    {
        Console.WriteLine("\nEscolha uma opção: 0 - Pedra ✊, 1 - Papel ✋ ou 2 - Tesoura ✌");
        char option = Console.ReadKey().KeyChar;
        while (option != '0' && option != '1' && option != '2')
        {
            Console.WriteLine("\nOpção inválida. Escolha 0, 1 ou 2.");
            option = Console.ReadKey().KeyChar;
        }
        return option;
    }

    /// <summary>
    /// AI strategy using minimax algorithm
    /// </summary>
    static int minimax(List<char> playersOptions, List<char> pcsOptions)
    {
        int playersPlayChoice;
        int pcsPlayChoice = 0;
        int pcsBestScore = -1000;
        int score;

        // Evaluate all possible moves
        Dictionary<int, int> utilityCount = new Dictionary<int, int> {
            {0, 0 },
            {1, 0 }
        };

        for (int playerIndex = 0; playerIndex < 2; playerIndex++)
        {
            for (int pcIndex = 0; pcIndex < 2; pcIndex++)
            {
                score = utility(playersOptions[playerIndex] - '0', pcsOptions[pcIndex] - '0');
                utilityCount[playerIndex] += score;
            }
        }

        // Predict player's most likely move
        playersPlayChoice = playersOptions[utilityCount.MinBy(keyValuePair => keyValuePair.Value).Key] - '0';

        // Choose computer's best response
        for (int pcIndex = 0; pcIndex < 2; pcIndex++)
        {
            int currentPcOption = pcsOptions[pcIndex] - '0';
            score = utility(playersPlayChoice, currentPcOption);
            if (score > pcsBestScore)
            {
                pcsBestScore = score;
                pcsPlayChoice = currentPcOption;
            }
        }

        return pcsPlayChoice;
    }

    /// <summary>
    /// Determines round winner
    /// </summary>
    static string winner(int playersPlayChoice, int pcsPlayChoice)
    {
        if (pcsPlayChoice == 0 && pcsPlayChoice + 2 == playersPlayChoice)  // Rock vs Scissors
        {
            return PC;
        }
        else if (pcsPlayChoice - 1 == playersPlayChoice)  // Paper vs Rock or Scissors vs Paper
        {
            return PC;
        }
        else if (pcsPlayChoice == playersPlayChoice)  // Draw
        {
            return null;
        }
        return PLAYER;  // Player wins other cases
    }

    /// <summary>
    /// Calculates utility score for minimax
    /// </summary>
    static int utility(int playersPlayChoice, int pcsPlayChoice)
    {
        string whoWon = winner(playersPlayChoice, pcsPlayChoice);
        if (whoWon == PC)
        {
            return 1;  // Computer wins
        }
        else if (whoWon == PLAYER)
        {
            return -1; // Player wins
        }
        else
        {
            return 0;  // Draw
        }
    }
}