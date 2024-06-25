using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

class Player
{
    static int globalTurn = 0;
    static int[] gamesScore;
    static int[,] scores = new int[3,13];
    static Game[] games = new Game[4];

    static string[] gpu;
    static int[] reg0, reg1, reg2, reg3, reg4, reg5, reg6;

    class Game
    {
        public int GameIndex;
        public int Turn;
        public bool MoveForWin;
        public bool ImpossibleWin;
        public bool AlreadyWin;

        public Game(int gameIndex)
        {
            GameIndex = gameIndex;
        }
    }

    class Node
    {
        public int ActionIndex;
        public int Turn;
        public Node PrevNode;

        public int PlayerPosition;
        public int PlayerStunTimer;

        public int X;
        public int Y;

        public int RollerScore;

        public int PlayerPoints;
        public int PlayerCombo;

        public double Score;

        public Node(int actionIndex, int turn, Node prevNode, 
            int playerPosition, int playerStunTimer, int x, int y,
            int rollerScore, int playerPoints, int playerCombo, double score)
        {
            ActionIndex = actionIndex;
            Turn = turn;
            PrevNode = prevNode;
            PlayerPosition = playerPosition;
            PlayerStunTimer = playerStunTimer;
            X = x;
            Y = y;
            RollerScore = rollerScore;
            PlayerPoints = playerPoints;
            PlayerCombo = playerCombo;
            Score = score;
        }
    }

    static void Main(string[] args)
    {
        int playerIndex = int.Parse(Console.ReadLine());
        int nbGames = int.Parse(Console.ReadLine());
        Log($"Player={playerIndex} Games={nbGames}");

        gamesScore = new int[nbGames];

        gpu = new string[nbGames];
        reg0 = new int[nbGames];
        reg1 = new int[nbGames];
        reg2 = new int[nbGames];
        reg3 = new int[nbGames];
        reg4 = new int[nbGames];
        reg5 = new int[nbGames];
        reg6 = new int[nbGames];

        for(int gameIndex = 0; gameIndex < nbGames; gameIndex++)
        {
            games[gameIndex] = new Game(gameIndex);
        }
        games[0].MoveForWin = true;
        games[1].MoveForWin = true;
        games[2].MoveForWin = true;

        while (true)
        {
            globalTurn++;

            for (int i = 0; i < 3; i++)
            {
                var playerScores = Console.ReadLine().Split(' ');
                for(int j = 0; j < 13; j++)
                {
                    scores[i,j] = int.Parse(playerScores[j]);
                }
            }
            Log($"Turn={globalTurn}");
            LogPlayerScores(0);
            LogPlayerScores(1);
            LogPlayerScores(2);

            for (int i = 0; i < nbGames; i++)
            {
                string[] inputs = Console.ReadLine().Split(' ');
                gpu[i] = inputs[0];
                reg0[i] = int.Parse(inputs[1]);
                reg1[i] = int.Parse(inputs[2]);
                reg2[i] = int.Parse(inputs[3]);
                reg3[i] = int.Parse(inputs[4]);
                reg4[i] = int.Parse(inputs[5]);
                reg5[i] = int.Parse(inputs[6]);
                reg6[i] = int.Parse(inputs[7]);

                Log($"{gpu[i]}", 3);
                Log($"{reg0[i]} {reg1[i]} {reg2[i]}", 3);
                Log($"{reg3[i]} {reg4[i]} {reg5[i]} {reg6[i]}", 3);
            }

            for(int gameIndex = 0; gameIndex < nbGames; gameIndex++)
            {
                if(gpu[gameIndex] == "GAME_OVER")
                {
                    games[gameIndex] = new Game(gameIndex);
                }

                games[gameIndex].Turn++;
                (bool impossibleWin, bool alreadyWin) = gameIndex switch
                {
                    0 => GetAbilityRaceWin(playerIndex),
                    1 => GetAbilityArcheryWin(playerIndex),
                    2 => GetAbilityRollerWin(playerIndex),
                    _ => GetAbilityDivingWin(playerIndex)
                };

                if(impossibleWin || alreadyWin)
                {
                    games[gameIndex].MoveForWin = false;
                }

                games[gameIndex].ImpossibleWin = impossibleWin;
                games[gameIndex].AlreadyWin = alreadyWin;
            }

            int moveForWinCount = games.Count(g => g.MoveForWin);
            if(moveForWinCount < 4)
            {
                var game = games
                    .Where(g => !g.MoveForWin && !g.ImpossibleWin && !g.AlreadyWin)
                    .OrderBy(g => GetGameScores(playerIndex, g.GameIndex))
                    .FirstOrDefault();
                
                if(game != null)
                {
                    game.MoveForWin = true;
                }
            }

            for(int gameIndex = 0; gameIndex < nbGames; gameIndex++)
            {
                Log(games[gameIndex], gameIndex);
            }

            (var playerPosition, var playerStunTimer) = GetPlayer(playerIndex, 0);
            (var x, var y) = GetPlayerArchery(playerIndex, 1);
            (var playerPoints, var playerCombo) = GetPlayer(playerIndex, 3);

            var queue = new Queue<Node>();

            var nodeUP = new Node(0, 0, null, playerPosition, playerStunTimer, x, y, 0, playerPoints, playerCombo, 0);
            queue.Enqueue(nodeUP);
            queue.Enqueue(new Node(1, 0, null, playerPosition, playerStunTimer, x, y, 0, playerPoints, playerCombo, 0));
            queue.Enqueue(new Node(2, 0, null, playerPosition, playerStunTimer, x, y, 0, playerPoints, playerCombo, 0));
            queue.Enqueue(new Node(3, 0, null, playerPosition, playerStunTimer, x, y, 0, playerPoints, playerCombo, 0));

            Node bestNode = nodeUP;

            while(queue.Count > 0)
            {
                var node = queue.Dequeue();    
      
                var raceScore = GetRaceScore(playerIndex, node.ActionIndex, ref node.PlayerPosition, ref node.PlayerStunTimer);
                var archeryScore = GetArcheryScore(playerIndex, node.ActionIndex, node.Turn, ref node.X, ref node.Y);
                var rollerScore = node.Turn == 0 ? GetRollerScore(playerIndex, node.ActionIndex) : node.RollerScore;           
                var divingScore = GetDivingScore(playerIndex, node.ActionIndex, node.Turn, ref node.PlayerPoints, ref node.PlayerCombo);

                node.Score = (node.Score + raceScore + archeryScore + rollerScore + divingScore) / (node.Turn + 1);
                //node.Score += (raceScore + archeryScore + rollerScore + divingScore) / (node.Turn + 1);
                //Log($"{GetActionLetter(node.ActionIndex)} {node.Score}: {raceScore} {archeryScore} {rollerScore} {divingScore}", 2);

                if(node.Score > bestNode.Score)
                {
                    bestNode = node;
                }

                if(node.Turn > 1 && (node.Score < bestNode.Score / 3 || node.Score == 0))
                    continue;

                if(node.Turn == 6)
                    continue; 

                queue.Enqueue(new Node(0, node.Turn + 1, node, node.PlayerPosition, node.PlayerStunTimer, node.X, node.Y, node.RollerScore, node.PlayerPoints, node.PlayerCombo, node.Score));
                queue.Enqueue(new Node(1, node.Turn + 1, node, node.PlayerPosition, node.PlayerStunTimer, node.X, node.Y, node.RollerScore, node.PlayerPoints, node.PlayerCombo, node.Score));
                queue.Enqueue(new Node(2, node.Turn + 1, node, node.PlayerPosition, node.PlayerStunTimer, node.X, node.Y, node.RollerScore, node.PlayerPoints, node.PlayerCombo, node.Score));
                queue.Enqueue(new Node(3, node.Turn + 1, node, node.PlayerPosition, node.PlayerStunTimer, node.X, node.Y, node.RollerScore, node.PlayerPoints, node.PlayerCombo, node.Score));
            }

            while(bestNode.Turn != 0)
            {
                bestNode = bestNode.PrevNode;
            }

            var action = bestNode.ActionIndex switch
            {
                0 => "UP",
                1 => "LEFT",
                2 => "DOWN",
                _ => "RIGHT"
            };

            Console.WriteLine($"{action} RolloTomasi go go go )");

            if(globalTurn == 100)
            {
                Log("GAME_END");
                break;
            }
        }
    }

    static int GetRaceScore(int playerIndex, int actionIndex, ref int playerPosition, ref int playerStunTimer)
    {
        const int gameIndex = 0;
        if(gpu[gameIndex] == "GAME_OVER" || !games[gameIndex].MoveForWin)
        {
            return 0;
        }

        if(playerStunTimer > 0)
        {
            playerStunTimer--;
            return 0;
        }

        int raceScore = 0;
        int index = playerPosition + 1;
        for(int moveCount = 0;
        index < gpu[gameIndex].Length && moveCount < (actionIndex != 0 ? actionIndex : 2); 
        index++, moveCount++)
        {
            raceScore++;

            if(actionIndex == 0 && moveCount == 0)
            {
                // UP
                continue;
            }

            if(gpu[gameIndex][index] == '#')
            {
                playerStunTimer = 3;
                raceScore = -1;
                break;
            }
        }

        playerPosition = index;
        return gpu[gameIndex].Length - playerPosition < 10 ? raceScore * 2 : raceScore;
    }

    static int GetArcheryScore(int playerIndex, int actionIndex, int turn, ref int x, ref int y)
    {
        const int gameIndex = 1;
        if(gpu[gameIndex] == "GAME_OVER" || !games[gameIndex].MoveForWin)
        {
            return 0;
        }

        int length = gpu[gameIndex].Length;
        if(turn >= length)
        {
            return 0;
        }

        int wind = gpu[gameIndex][turn] - '0';

        int x2 = x;
        int y2 = y;

        var actionLetter = GetActionLetter(actionIndex);
        switch(actionLetter)
        {
            case 'U':
                y2 -= wind;
                break;
            case 'L':
                x2 -= wind;
                break;
            case 'D':
                y2 += wind;
                break;
            case 'R':
            default:
                x2 += wind;
                break;                
        }

        x = x2;
        y = y2;

        Log($"wind={wind} x={x} x2={x2} y={y} y2={y2}");

        if(length - turn > 1)
        {
            var arr = gpu[gameIndex].Skip(turn + 1).Select(a => a - '0').OrderByDescending(a => a).ToArray();
            for(int i = 0; i < arr.Length; i++)
            {
                if(Math.Abs(x2) > Math.Abs(y2))
                {
                    x2 = x2 > 0 ? x2 - arr[i] : x2 + arr[i];
                }
                else
                {
                    y2 = y2 > 0 ? y2 - arr[i] : y2 + arr[i];
                }
            }
        }

        var archeryScore = CalcArcheryScore(x2, y2);
        return gpu[gameIndex].Length <= 5 ? archeryScore * 2 : archeryScore;
    }

    static int CalcArcheryScore(int x2, int y2)
    {
        var sum = Math.Abs(x2) + Math.Abs(y2);
        switch(sum)
        {
            case 0:
                return 3;
            case 1:
                return 2;
            case 2:
                return 1;
            case 3:
                return 0;
            default:
                return -1;
        }
    }

    static int GetRollerScore(int playerIndex, int actionIndex)
    {
        const int gameIndex = 2;
        if(gpu[gameIndex] == "GAME_OVER" || !games[gameIndex].MoveForWin)
        {
            return 0;
        }

        int turnsLeft = reg6[gameIndex];
        bool notLastTurn = turnsLeft > 1;

        (var playerPosition, var playerStunRisk) = GetPlayer(playerIndex, gameIndex);
        if(playerStunRisk < 0)
        {
            return 0;
        }

        int rollerScore = 0;
        var actionLetter = GetActionLetter(actionIndex);
        int rollerRiskIndex = 0;
        for(; rollerRiskIndex < gpu[gameIndex].Length; rollerRiskIndex++)
        {
            if(gpu[gameIndex][rollerRiskIndex] == actionLetter)
            {
                break;
            }
        }

        switch(rollerRiskIndex)
        {
            case 0:
                rollerScore = 1;
                break;
            case 1:
                rollerScore = 2;
                break;
            case 2:
                rollerScore = notLastTurn && playerStunRisk >= 4 ? 0 : 2;
                break;
            default:
                rollerScore = notLastTurn && playerStunRisk >= 3 ? 0 : 3;
                break;
        };

        return rollerScore;
    }

    static int GetDivingScore(int playerIndex, int actionIndex, int turn, ref int playerPoints, ref int playerCombo)
    {
        const int gameIndex = 3;
        if(gpu[gameIndex] == "GAME_OVER" || !games[gameIndex].MoveForWin)
        {
            return 0;
        }

        int length = gpu[gameIndex].Length;
        if(turn >= length)
        {
            return 0;
        }

        int divingScore = 0;
        var actionLetter = GetActionLetter(actionIndex);
        if(gpu[gameIndex][turn] == actionLetter)
        {
            divingScore = playerCombo > 0 ? 2 : 1;

            playerCombo++;
            playerPoints += playerCombo;
        }
        else
        {
            playerCombo = 0;
        }

        return length <= 5 ? divingScore * 2 : divingScore;
    }

    static (bool impossibleWin, bool alreadyWin) GetAbilityRaceWin(int playerIndex)
    {
        const int gameIndex = 0;
        bool impossibleWin = false;
        bool alreadyWin = false;


        if((gpu[gameIndex].Length / 3) + globalTurn > 100)
        {
            return (true, alreadyWin);
        }

        return (impossibleWin, alreadyWin);
    }

    static (bool impossibleWin, bool alreadyWin) GetAbilityArcheryWin(int playerIndex)
    {
        const int gameIndex = 1;
        bool impossibleWin = false;
        bool alreadyWin = false;

        if(gpu[gameIndex].Length + globalTurn > 100)
        {
            return (true, alreadyWin);
        }

        return (impossibleWin, alreadyWin);
    }

    static (bool impossibleWin, bool alreadyWin) GetAbilityRollerWin(int playerIndex)
    {
        const int gameIndex = 2;
        bool impossibleWin = false;
        bool alreadyWin = false;

        int turnsLeft = reg6[gameIndex];
        if(turnsLeft + globalTurn > 100)
        {
            return (true, alreadyWin);
        }

        (var playerPosition, var playerStunRisk) = GetPlayer(playerIndex, gameIndex);

        int maxPlayerPosition = playerPosition;
        int maxPlayerStunRisk = playerStunRisk;

        (int[] enemyPosition, int[] enemyStunRisk) = GetEnemies(playerIndex, gameIndex);

        int[] maxEnemyPosition = enemyPosition.ToArray();
        int[] maxEnemyStunRisk = enemyStunRisk.ToArray();

        for(int turn = 0; turn < turnsLeft; turn++)
        {
            if(maxPlayerStunRisk <= 2)
            {
                maxPlayerStunRisk += 2;
                maxPlayerPosition += 3;
            }
            else
            {
                maxPlayerPosition += 2;
            }

            for(int j = 0; j < 2; j++)
            {
                if(maxEnemyStunRisk[j] < 0)
                {
                    maxEnemyStunRisk[j]++;
                    continue;
                }

                if(maxEnemyStunRisk[j] <= 2)
                {
                    maxEnemyStunRisk[j] += 2;
                    maxEnemyPosition[j] += 3;
                }
                else
                {
                    maxEnemyPosition[j] += 2;
                }
            }
        }

        if(maxPlayerPosition < enemyPosition[0] && maxPlayerPosition < enemyPosition[1])
        {
            impossibleWin = true;
        }

        if(playerPosition >= maxEnemyPosition[0] && playerPosition >= maxEnemyPosition[1])
        {
            alreadyWin = true;
        }     

        return (impossibleWin, alreadyWin);
    }

    static (bool impossibleWin, bool alreadyWin) GetAbilityDivingWin(int playerIndex)
    {
        const int gameIndex = 3;
        bool impossibleWin = false;
        bool alreadyWin = false;

        if(gpu[gameIndex].Length + globalTurn > 100)
        {
            return (true, alreadyWin);
        }

        (var playerPoints, var playerCombo) = GetPlayer(playerIndex, gameIndex);

        int maxPlayerPoints = playerPoints;
        int maxPlayerCombo = playerCombo;

        (int[] enemyPoints, int[] enemyCombo) = GetEnemies(playerIndex, gameIndex);

        int[] maxEnemyPoints = enemyPoints.ToArray();
        int[] maxEnemyCombo = enemyCombo.ToArray();

        for(int i = 0; i < gpu[gameIndex].Length; i++)
        {
            maxPlayerPoints += maxPlayerCombo;
            maxPlayerCombo++;

            for(int j = 0; j < 2; j++)
            {
                maxEnemyPoints[j] += maxEnemyCombo[j];
                maxEnemyCombo[j]++;
            }
        }

        if(maxPlayerPoints < enemyPoints[0] && maxPlayerPoints < enemyPoints[1])
        {
            impossibleWin = true;
        }

        if(playerPoints >= maxEnemyPoints[0] && playerPoints >= maxEnemyPoints[1])
        {
            alreadyWin = true;
        }

        return (impossibleWin, alreadyWin);
    }        

    static (int playerReg012, int playerReg345) GetPlayer(int playerIndex, int gameIndex) =>
        (GetReg012(playerIndex, gameIndex), GetReg345(playerIndex, gameIndex));

    static (int playerReg024, int playerReg135) GetPlayerArchery(int playerIndex, int gameIndex) =>
        (GetReg024(playerIndex, gameIndex), GetReg135(playerIndex, gameIndex));

    static (int[] enemyReg012, int[] enemyReg345) GetEnemies(int playerIndex, int gameIndex)
    {
        var enemyReg012 = new int[2];
        var enemyReg345 = new int[2];
        for(int i = 0, j = 0; i < 3; i++)
        {
            if(i == playerIndex)
                continue;

            enemyReg012[j] = GetReg012(i, gameIndex);
            enemyReg345[j] = GetReg345(i, gameIndex);
            j++;
        }

        return (enemyReg012, enemyReg345);
    }

    static (int[] enemyReg024, int[] enemyReg135) GetEnemiesArchery(int playerIndex, int gameIndex)
    {
        var enemyReg024 = new int[2];
        var enemyReg135 = new int[2];
        for(int i = 0, j = 0; i < 3; i++)
        {
            if(i == playerIndex)
                continue;

            enemyReg024[j] = GetReg024(i, gameIndex);
            enemyReg135[j] = GetReg135(i, gameIndex);
            j++;
        }

        return (enemyReg024, enemyReg135);
    }

    static int GetReg012(int playerIndex, int gameIndex) =>
        playerIndex switch
        {
            0 => reg0[gameIndex],
            1 => reg1[gameIndex],
            _ => reg2[gameIndex]
        };

    static int GetReg345(int playerIndex, int gameIndex) =>
        playerIndex switch
        {
            0 => reg3[gameIndex],
            1 => reg4[gameIndex],
            _ => reg5[gameIndex]
        };

    static int GetReg024(int playerIndex, int gameIndex) =>
        playerIndex switch
        {
            0 => reg0[gameIndex],
            1 => reg2[gameIndex],
            _ => reg4[gameIndex]
        };

    static int GetReg135(int playerIndex, int gameIndex) =>
        playerIndex switch
        {
            0 => reg1[gameIndex],
            1 => reg3[gameIndex],
            _ => reg5[gameIndex]
        };

    static char GetActionLetter(int actionIndex) =>
        actionIndex switch
        {
            0 => 'U',
            1 => 'L',
            2 => 'D',
            _ => 'R'
        };

    static string GetGameName(int gameIndex) =>
        gameIndex switch
        {
            0 => "Race",
            1 => "Archery",
            2 => "Roller",
            _ => "Diving"
        };

    static void LogPlayerScores(int playerIndex)
    {
        Log($"Player{playerIndex} Scores: {scores[playerIndex,0]} {CalcPlayerScores(playerIndex)}", 5);
        Log($"Race {scores[playerIndex,1]} {scores[playerIndex,2]} {scores[playerIndex,3]}", 5);
        Log($"Archery {scores[playerIndex,4]} {scores[playerIndex,5]} {scores[playerIndex,6]}", 5);
        Log($"Roller {scores[playerIndex,7]} {scores[playerIndex,8]} {scores[playerIndex,9]}", 5);
        Log($"Diving {scores[playerIndex,10]} {scores[playerIndex,11]} {scores[playerIndex,12]}", 5);
    }

    static int CalcPlayerScores(int playerIndex) =>
        (scores[playerIndex,1] * 3 + scores[playerIndex,2]) *
        (scores[playerIndex,4] * 3 + scores[playerIndex,5]) *
        (scores[playerIndex,7] * 3 + scores[playerIndex,8]) *
        (scores[playerIndex,10] * 3 + scores[playerIndex,11]);

    static int SumPlayerScores(int playerIndex) =>
        (scores[playerIndex,1] * 3 + scores[playerIndex,2]) +
        (scores[playerIndex,4] * 3 + scores[playerIndex,5]) +
        (scores[playerIndex,7] * 3 + scores[playerIndex,8]) +
        (scores[playerIndex,10] * 3 + scores[playerIndex,11]);

    static int GetGameScores(int playerIndex, int gameIndex)
    {
        return scores[playerIndex,1 + gameIndex * 3] * 3 +
            scores[playerIndex,2 + gameIndex * 3];
    }

    static void Log(Game game, int gameIndex)
    {
        Log($"{GetGameName(gameIndex)} {game.Turn}: {game.MoveForWin} {game.ImpossibleWin} {game.AlreadyWin}", 3);
    }

    static void Log(string message, int level = 0)
    {
        if(level < 7)
        {
            return;
        }

        Console.Error.WriteLine(message);
    }
}