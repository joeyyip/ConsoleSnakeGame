using System;
using System.Threading;

namespace Snakespace {

    struct Graphics {
        public const char empty = '\0';
        public const char wall = 'x';
        public const char snake = '*';
        public const char food = '+';
        public const char hazard = '@';
    }

    class Snake {
        public enum Direction { Up, Down, Left, Right };
        public Direction snakeDirection; /* Direction of snake's movement */
        public int xPos, yPos; /* The position of the snake's head */
        private Game game; /* Reference to the game object */

        /* Initialize a snake at the specified position, passing in
         * the calling game object. 
         */
        public Snake(int xPos, int yPos, Game game) {
            this.snakeDirection = Direction.Right;
            this.game = game;
            this.xPos = xPos;
            this.yPos = yPos;

            game.draw(xPos, yPos, Graphics.snake);
        }

        /* Move the snake one step in the specified direction.
         * Returns true on success, false on collision.
         */
        public bool move(Direction moveDirection) {
            int xNew, yNew;
            char symbol;

            /* Invalid changes in direction */
            if (((moveDirection == Direction.Up) && (snakeDirection == Direction.Down)) ||
                ((moveDirection == Direction.Down) && (snakeDirection == Direction.Up)) ||
                ((moveDirection == Direction.Left) && (snakeDirection == Direction.Right)) ||
                ((moveDirection == Direction.Right) && (snakeDirection == Direction.Left))) {
                moveDirection = snakeDirection;
            }

            switch (moveDirection) {
                case Direction.Left:
                    xNew = xPos - 1;
                    yNew = yPos;
                    break;
                case Direction.Right:
                    xNew = xPos + 1;
                    yNew = yPos;
                    break;
                case Direction.Up:
                    xNew = xPos;
                    yNew = yPos - 1;
                    break;
                case Direction.Down:
                    xNew = xPos;
                    yNew = yPos + 1;
                    break;
                default:
                    xNew = xPos;
                    yNew = yPos;
                    break;
            }

            /* Get the symbol at the position the snake will be moving to */
            symbol = game.getPlayAreaSymbol(xNew, yNew);

            /* Detect a collision */
            if ((symbol != Graphics.empty) && (symbol != Graphics.food)) {
                return false;
            }

            /* Increment score */
            if (symbol == Graphics.food) {
                game.playerScore += Game.foodBonus;
            } else {
                game.playerScore++;
            }

            /* Move the snake */
            game.draw(xNew, yNew, Graphics.snake);
            xPos = xNew;
            yPos = yNew;
            snakeDirection = moveDirection;
            return true;
        }
    }

    class Game {
        /* Configuration values
         */
        private int playWidth = 30; /* Width of play area */
        private int playHeight = 30; /* Height of play area */
        private int numFood = 10; /* Number of food items */
        private int numHazards = 10; /* Number of hazards */

        private const int messageWidth = 60; /* Width of message area */
        private const int messageHeight = 10; /* Height of message area */
        private const int snakeDelay = 100; /* Milliseconds between a snake's "step" */
        private const int readInputDelay = 5; /* Milliseconds between reading user input */
        public const int foodBonus = 10; /* Bonus score increment for food items */

        private Snake snake;
        private DateTime snakeNextMove;
        private bool exitGame = false;
        private char[,] playArea; /* The internal memory of the play area */
        public int playerScore = 0;

        /* Initialize a game.
         */
        public Game(string[] args) {
            int idx;
            int windowWidth, windowHeight;
            int minWindowWidth, minWindowHeight;

            /* Process command line arguments */
            for (idx = 0; idx < args.Length; idx++) {
                if (args[idx].ToLower().Equals("--more-hazards")) {
                    numHazards = 100;
                }
            }

            /* Determine minimum window size */
            minWindowWidth = (playWidth > messageWidth) ? playWidth : messageWidth;
            minWindowHeight = playHeight + messageHeight;

            /* Setup the console */
            Console.CursorVisible = false;
            Console.Title = "Snake Snake Snake";

            /* Setup window size */
            windowWidth = Console.WindowWidth;
            windowHeight = Console.WindowHeight;
            if (windowWidth < minWindowWidth) windowWidth = minWindowWidth;
            if (windowHeight < minWindowHeight) windowHeight = minWindowHeight;
            Console.SetWindowSize(windowWidth, windowHeight);

            playArea = new char[playWidth, playHeight];
        }

        /* Draw the char symbol at the specified position
         */
        public void draw(int xPos, int yPos, char symbol) {
            playArea[xPos, yPos] = symbol;
            Console.SetCursorPosition(xPos, yPos);
            Console.Write(symbol);
        }

        /* Return the char symbol at the specified position
         */
        public char getPlayAreaSymbol(int xPos, int yPos) {
            return playArea[xPos, yPos];
        }

        /* Displays the players score
         */
        private void displayScore() {
            string message = String.Format("Score: {0}", playerScore.ToString());
            Console.SetCursorPosition(0, playArea.GetLength(1) + 1);
            Console.WriteLine(message.PadRight(messageWidth));
        }

        /* Display a message
         */
        private void displayMessage(string message) {
            Console.SetCursorPosition(0, playArea.GetLength(1) + 3);
            Console.WriteLine("".PadRight(messageWidth));
            Console.WriteLine("".PadRight(messageWidth));
            Console.WriteLine("".PadRight(messageWidth));
            Console.WriteLine("".PadRight(messageWidth));
            Console.WriteLine("".PadRight(messageWidth));
            Console.WriteLine("".PadRight(messageWidth));
            Console.SetCursorPosition(0, playArea.GetLength(1) + 3);
            Console.WriteLine(message);
        }

        /* Setup a new game
         */
        public void gameReset(bool displayInstructions) {
            int idx, jdx;
            int foodCount = 0, hazardCount = 0;
            Random rnd = new Random();
            ConsoleKeyInfo input;

            Console.Clear();
            playerScore = 0;

            /* Initialize the play area */
            for (idx = 0; idx < playArea.GetLength(0); idx++) {
                for (jdx = 0; jdx < playArea.GetLength(1); jdx++) {
                    if ((jdx == 0) || (jdx == (playArea.GetLength(1) - 1)) ||
                            (idx == 0) || (idx == (playArea.GetLength(0) - 1))) {
                        draw(idx, jdx, Graphics.wall);
                    } else {
                        draw(idx, jdx, Graphics.empty);
                    }
                }
            }

            /* Start the snake in the middle of the play area */
            snake = new Snake(playArea.GetLength(0) / 2, playArea.GetLength(1) / 2, this);

            /* Generate the food bonuses */
            while (foodCount < numFood) {
                idx = rnd.Next(1, playArea.GetLength(0) - 1);
                jdx = rnd.Next(1, playArea.GetLength(1) - 1);

                if (getPlayAreaSymbol(idx, jdx) != Graphics.empty) {
                    continue;
                }

                draw(idx, jdx, Graphics.food);
                foodCount++;
            }

            /* Generate the hazards */
            while (hazardCount < numHazards) {
                idx = rnd.Next(1, playArea.GetLength(0) - 1);
                jdx = rnd.Next(1, playArea.GetLength(1) - 1);

                /* Avoid placing a hazard in the "starting line" of the snake */
                if ((jdx == snake.yPos) && (idx >= snake.xPos)) {
                    continue;
                }

                if (getPlayAreaSymbol(idx, jdx) != Graphics.empty) {
                    continue;
                }

                draw(idx, jdx, Graphics.hazard);
                hazardCount++;
            }

            /* Display instructions */
            if (displayInstructions == true) {
                displayMessage(String.Format(
                               "- Use WASD to steer the snake up, down, left, and right.\n" +
                               "- Collect food ({0}) for more points.\n" +
                               "- Avoid hazards ({1}) and keep moving to stay alive.\n\n" +
                               "Tip: Use a console font with equal height and width.\n" +
                               "Press 'p' to start playing.",
                               Graphics.food, Graphics.hazard));
                while (true) {
                    input = Console.ReadKey(true);

                    if (input.Key == ConsoleKey.P) {
                        displayMessage("");
                        break;
                    }
                }
            }

            displayScore();
        }

        /* The main loop for the game. The loop will:
         *  - read input from the user
         *  - move the snake one step in a direction
         */
        public void gameLoop() {
            ConsoleKeyInfo input;
            Snake.Direction direction = snake.snakeDirection;

            while (true) {
                if (exitGame == true) {
                    break;
                }

                /* Read all user input that has been buffered up to this point.
                 * Only the last valid input will be used. */
                while (Console.KeyAvailable == true) {
                    input = Console.ReadKey(true);

                    if (input.Key == ConsoleKey.W)
                        direction = Snake.Direction.Up;
                    else if (input.Key == ConsoleKey.S)
                        direction = Snake.Direction.Down;
                    else if (input.Key == ConsoleKey.A)
                        direction = Snake.Direction.Left;
                    else if (input.Key == ConsoleKey.D)
                        direction = Snake.Direction.Right;
                }

                if (DateTime.Now > snakeNextMove) {
                    /* Time for the snake to move one step */
                    if (snake.move(direction) == false) {
                        /* The snake hit something */
                        gameOver();

                        /* Reset the user's input. */
                        direction = snake.snakeDirection;
                    }
                    snakeNextMove = DateTime.Now.AddMilliseconds(snakeDelay);
                }

                displayScore();
                Thread.Sleep(readInputDelay);
            }
        }

        /* Exit the game
         */
        public void gameExit() {
            exitGame = true;
            displayMessage("Terminating game...");
            Environment.Exit(1);
        }

        /* Display game over message
         */
        public void gameOver() {
            ConsoleKeyInfo input;

            displayMessage("Game Over. Press 'p' to play again. Press 'q' to quit.");

            while (true) {
                input = Console.ReadKey(true);

                if (input.Key == ConsoleKey.P) {
                    gameReset(false);
                    break;
                }
                if (input.Key == ConsoleKey.Q) {
                    gameExit();
                    break;
                }
            }
        }
    }

    class Program {
        static Game game;

        static void Main(string[] args) {
            /* Terminate gracefully when user hits ^C */
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

            game = new Game(args);
            game.gameReset(true);
            game.gameLoop();
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) {
            e.Cancel = true;
            game.gameExit();
        }
    }
}
