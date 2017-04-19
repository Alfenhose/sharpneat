/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2016 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software; you can redistribute it and/or modify
 * it under the terms of The MIT License (MIT).
 *
 * You should have received a copy of the MIT License
 * along with SharpNEAT; if not, see https://opensource.org/licenses/MIT.
 */
using System;
using System.Collections;
using System.IO;
using Redzen.Numerics;
using SharpNeat.Phenomes;
using System.Collections.Generic;

namespace SharpNeat.Domains.Spelunky
{
    /// <summary>
    /// The prey capture task's grid world. Encapsulates agent's sensor and motor hardware and the prey's simple stochastic movement.
    /// </summary>
    public class SpelunkyGenerator
    {
        #region Constants

        const int wall = 1;
        const int air = 0;

        #endregion

        #region Instance Fields

        // World parameters.

        readonly int _gridWidth;
        readonly int _gridHeight;
        readonly int _mooreSize;
        readonly double _initPercentage;    //how much of the inital world should be filled
        readonly int _steps;            // how many steps should the generator perform before showing the result

        // World state.
        int[,] _world;
        IntPoint _startPos;
        IntPoint _endPos;
        
        public ArrayList Rooms { get; set; }

        //for the evaluator
        double percentage = -1;
        int[,] _integralWorld;
        bool generated = false;
        bool stats = false;
        int _nooks;
        int _ends;
        int _solids;
        int _empties;
        int _loners;
        int _holes;
        int _pits;
        int _tunnels;
        int _spires;
        int _platforms;

        int _horizontal;
        int _vertical;

        // Random number generator.
        Random _rng;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs with the provided world parameter arguments.
        /// </summary>
        public SpelunkyGenerator(int gridWidth, int gridHeight, double initPercentage, int mooreSize, int steps)
        {
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _initPercentage = initPercentage / 100.0;
            _mooreSize = mooreSize;
            _steps = steps;
            _rng = new Random();
            _startPos = new IntPoint(1, 1);
            _endPos = new IntPoint(_gridWidth - 2, _gridHeight - 2);
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets the width of the grid in terms of number of squares.
        /// </summary>
        public int GridWidth
        {
            get { return _gridWidth; }
        }
        /// <summary>
        /// Gets the height of the grid in terms of number of squares.
        /// </summary>
        public int GridHeight
        {
            get { return _gridHeight; }
        }
        /// <summary>
        /// Gets the percentage of cells wich contain air
        /// </summary>
        public double InitPercentage
        {
            get { return _initPercentage; }
        }
        /// <summary>
        /// Gets the amount of steps being performed.
        /// </summary>
        public int Steps
        {
            get { return _steps; }
        }
        /// <summary>
        /// Gets the percentage of filled blocks.
        /// </summary>
        public double Percentage
        {
            get
            {
                if (percentage >= 0)
                {
                    return percentage;
                }
                percentage = ((float)(IntegralWorld[GridWidth - 1, GridHeight - 1])) / ((float)(GridWidth * GridHeight));
                return percentage;
            }
        }
        /// <summary>
        /// Gets the integral of the world.
        /// </summary>
        public int[,] IntegralWorld
        {
            get
            {
                if (_integralWorld != null)
                {
                    return _integralWorld;
                }
                _integralWorld = CalcIntegralWorld(World);
                return _integralWorld;
            }
            private set { _integralWorld = value; }
        }
        /// <summary>
        /// Gets the starting position.
        /// </summary>
        public IntPoint StartPos
        {
            get { return _startPos; }
            private set { _startPos = value; }
        }
        /// <summary>
        /// Gets the End position.
        /// </summary>
        public IntPoint EndPos
        {
            get { return _endPos; }
            private set { _endPos = value; }
        }
        /// <summary>
        /// Returns the number of Loners (single walls surrounded by air).
        /// </summary>
        public int Loners
        {
            get
            {
                if (!stats)
                {
                    CalcStats(World);
                }
                return _loners;
            }
        }
        /// <summary>
        /// Returns the number of Holes (single empty spaces surrounded by walls).
        /// </summary>
        public int Holes
        {
            get
            {
                if (!stats)
                {
                    CalcStats(World);
                }
                return _holes;
            }
        }
        /// <summary>
        /// Returns the number of Solids (walls surrounded completely by walls).
        /// </summary>
        public int Solids
        {
            get
            {
                if (!stats)
                {
                    CalcStats(World);
                }
                return _solids;
            }
        }
        /// <summary>
        /// Returns the number of Empties (empty spaces surrounded completely by empty space).
        /// </summary>
        public int Empties
        {
            get
            {
                if (!stats)
                {
                    CalcStats(World);
                }
                return _empties;
            }
        }
        /// <summary>
        /// Returns the number of Nooks (empty spaces surrounded on three sides by walls).
        /// </summary>
        public int Nooks
        {
            get
            {
                if (!stats)
                {
                    CalcStats(World);
                }
                return _nooks;
            }
        }
        /// <summary>
        /// Returns the number of Ends (walls connected to a single wall).
        /// </summary>
        public int Ends
        {
            get
            {
                if (!stats)
                {
                    CalcStats(World);
                }
                return _ends;
            }
        }
        /// <summary>
        /// Returns the number of Pits (empty space with wall on the left and right).
        /// </summary>
        public int Pits
        {
            get
            {
                if (!stats)
                {
                    CalcStats(World);
                }
                return _pits;
            }
        }
        /// <summary>
        /// Returns the number of Tunnels (empty space with wall on top and on bottom).
        /// </summary>
        public int Tunnels
        {
            get
            {
                if (!stats)
                {
                    CalcStats(World);
                }
                return _ends;
            }
        }
        /// <summary>
        /// Returns the number of Platforms (wall with wall on the left and right).
        /// </summary>
        public int Platforms
        {
            get
            {
                if (!stats)
                {
                    CalcStats(World);
                }
                return _pits;
            }
        }
        /// <summary>
        /// Returns the number of Spires (wall with wall on top and on bottom).
        /// </summary>
        public int Spires
        {
            get
            {
                if (!stats)
                {
                    CalcStats(World);
                }
                return _ends;
            }
        }
        /// <summary>
        /// Returns the number of horizontal walls (wall with air on top or on bottom).
        /// </summary>
        public int Horizontals
        {
            get
            {
                if (!stats)
                {
                    CalcStats(World);
                }
                return _horizontal;
            }
        }
        /// <summary>
        /// Returns the number of vertical walls (wall with air on at least one side).
        /// </summary>
        public int Verticals
        {
            get
            {
                if (!stats)
                {
                    CalcStats(World);
                }
                return _vertical;
            }
        }
        /// <summary>
        /// Gets the integral of the world.
        /// </summary>
        public int[,] World
        {
            get
            {
                if (_world != null)
                {
                    return _world;
                }
                _world = new int[GridWidth, GridHeight];
                return _world;
            }
            private set { _world = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates the world
        /// </summary>
        public void GenerateWorld()
        {
            // Init world state.
            Reset();
            SetUpRooms();
            // make a random world by filling it with noise
            RandomizeWorld(World);

            generated = true;
        }
        /// <summary>
        /// Generates the world
        /// </summary>
        public void SaveWorld(StreamWriter file)
        {
            if (!generated)
            {
                GenerateWorld();
            }
            else {
                for (int y = 0; y < _gridHeight; y++)
                {
                    for (int x = 0; x < _gridWidth; x++)
                    {
                        if (StartPos._x == x && StartPos._y == y)
                        {
                            file.Write("@");
                        }
                        else if (EndPos._x == x && EndPos._y == y)
                        {
                            file.Write("X");
                        }
                        else
                        {
                            file.Write(GetWorldPoint(x, y));
                        }
                    }
                    file.WriteLine();
                }
                file.WriteLine("Mikkel Balslev");
                file.WriteLine("Generated Level");
                file.WriteLine("4");
                file.WriteLine("4");
                file.WriteLine("4");
                file.WriteLine("NONE");
                file.WriteLine("2");
                file.WriteLine();
                file.WriteLine();
                file.WriteLine("0");
            }
        }
        /// <summary>
        /// Get value at coordinates
        /// </summary>
        public int GetWorldPoint(int x, int y)
        {
            if (x < 0 || y < 0 || x >= GridWidth || y >= GridHeight)
            {
                return wall;
            }
            return World[x, y];
        }

        /// <summary>
        /// Runs one trial of the provided agent in the world. Returns true if the agent captures the prey within
        /// the maximum number of timesteps allowed.
        /// returns true if every result is between 0 and 1
        /// returns false if any result is less than 0 or greater than 1
        /// </summary>
        public bool ShapeTheWorld(IBlackBox agent)
        {
            bool consistent = true;
            var tempWorld = new int[_gridWidth, _gridHeight];
            // Let the chase begin!
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    // Reset all inputs.
                    agent.InputSignalArray.Reset();
                    //set inputs for the spot
                    SetAgentInputs(agent, new IntPoint(x, y));
                    //make the agent do the calculations
                    agent.Activate();

                    double value = agent.OutputSignalArray[0];
                    tempWorld[x, y] = (int)Math.Round(value);
                    if (value < 0 || value > 1) { consistent = false; }
                }
            }
            World = tempWorld;
            return consistent;
        }

        /// <summary>
        /// counts loners and the like of world
        /// </summary>
        public void CalcStats(int[,] world)
        {

            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    int temp = 0;
                    // four bit value NSEW, north south east west
                    // first bit is North
                    // second is south
                    // third is east
                    // fourth is west
                    temp += 1 * GetWorldPoint(x, y - 1);
                    temp += 2 * GetWorldPoint(x, y + 1);
                    temp += 4 * GetWorldPoint(x - 1, y);
                    temp += 8 * GetWorldPoint(x + 1, y);

                    // when looking at wall
                    if (GetWorldPoint(x, y) == 1)
                    {
                        switch (temp)
                        {
                            case 0:
                                _loners++;
                                break;
                            case 1:
                                _ends++;
                                break;
                            case 2:
                                _ends++;
                                break;
                            case 3:
                                _platforms++;
                                _horizontal++;
                                break;
                            case 4:
                                _ends++;
                                break;
                            case 7:
                                _vertical++;
                                break;
                            case 8:
                                _ends++;
                                break;
                            case 11:
                                _vertical++;
                                break;
                            case 12:
                                _spires++;
                                _vertical++;
                                break;
                            case 13:
                                _horizontal++;
                                break;
                            case 14:
                                _horizontal++;
                                break;
                            case 15:
                                _solids++;
                                break;
                            default:
                                break;
                        }
                    }

                    // when looking at empty space
                    if (GetWorldPoint(x, y) == 0)
                    {
                        switch (temp)
                        {
                            case 0:
                                _empties++;
                                break;
                            case 3:
                                _pits++;
                                break;
                            case 7:
                                _nooks++;
                                break;
                            case 11:
                                _nooks++;
                                break;
                            case 12:
                                _tunnels++;
                                break;
                            case 13:
                                _nooks++;
                                break;
                            case 14:
                                _nooks++;
                                break;
                            case 15:
                                _holes++;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determine the agent's position in the world relative to the prey and walls, and set its sensor inputs accordingly.
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="pos"></param>
        public void SetAgentInputs(IBlackBox agent, IntPoint pos)
        {
            // Determine agent sensor input values.
            int x, y;
            int index = 0;
            for (int dx = -_mooreSize; dx <= _mooreSize; dx++)
            {
                for (int dy = -_mooreSize; dy <= _mooreSize; dy++)
                {
                    x = pos._x + dx;
                    y = pos._y + dy;
                    //check if coordinates are outside the bounds
                    if (x < 0 || y < 0 || x >= GridWidth || y >= GridHeight)
                    {
                        //if the position is outside the world we set the input to look like there is a wall
                        agent.InputSignalArray[index] = wall;
                    }
                    else
                    {
                        //if inside the bounds we set the input to what ever is in that spot
                        agent.InputSignalArray[index] = GetWorldPoint(x, y);
                    }
                    index++;
                }
            }
        }

        /// <summary>
        /// Remove almost empty rooms and move the not empty rooms
        /// </summary>
        public void RecalculateRooms()
        {
            ArrayList temp = new ArrayList();
            foreach (Room room in Rooms)
            {
                int size = room.Tiles;
                if (size > 16)
                {
                    room.Position = new IntPoint(room.TilesPosition._x / room.Tiles, room.TilesPosition._y / room.Tiles);
                    room.Tiles = 0;
                    room.TilesPosition = new IntPoint(0, 0);

                    temp.Add(room);
                    /*
                    //An attempt at making the rooms multiply if they contain too many tiles
                    if (size > 24)
                    {
                        if (_rng.Next(2) == 0)
                        {
                            temp.Add(new Room(room.Position + new IntPoint(1, 0)));
                            temp.Add(new Room(room.Position + new IntPoint(-1, 0)));
                        }
                        else
                        {
                            temp.Add(new Room(room.Position + new IntPoint(0, 1)));
                            temp.Add(new Room(room.Position + new IntPoint(0, -1)));
                        }
                    }
                    else
                    {
                        temp.Add(room);
                    }*/
                }
            }
            Rooms = temp;
        }
        /// <summary>
        /// Calculate the rooms closest tiles,adding their locations together and keeping count of the amount
        /// </summary>
        public void CalculateRooms()
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (GetWorldPoint(x, y) == 0)
                    {
                        Room closest = null;
                        double distance = double.PositiveInfinity;
                        foreach (Room room in Rooms)
                        {
                            double newDistance = IntPoint.CalculateDistance(room.Position, new IntPoint(x, y));
                            if (newDistance < distance)
                            {
                                closest = room;
                                distance = newDistance;
                            }
                        }
                        if (closest != null)
                        {
                            closest.Tiles++;
                            closest.TilesPosition += new IntPoint(x, y);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculate the interconnections in between the rooms, place start and end and choose a route
        /// </summary>
        public void CalculateConnections()
        {
            ArrayList startrooms = new ArrayList();
            foreach (Room room in Rooms)
            {
                if (room.Position._y < 4)
                {
                    startrooms.Add(room);
                }

                foreach (Room neighbour in Rooms)
                {
                    if (!room.Equals(neighbour))
                    {
                        IntPoint distance = (neighbour.Position - room.Position);
                        int manhattanDistance = Math.Abs(distance._x) + Math.Abs(distance._y);
                        int chebyskovDistance = Math.Max(Math.Abs(distance._x), Math.Abs(distance._y));

                        if (distance._y >= -2 && manhattanDistance < 7)
                        {
                            room.AddConnection(neighbour);
                        }
                    }
                }
            }
            //now we have the connections now we make a route
            int highestDepth = 0;
            Room end = new Room(new IntPoint(5, 4));
            Room start = new Room(new IntPoint(_gridWidth-5, _gridHeight-4));

            //foreach (Room origin in startrooms) {
            if (startrooms.Count > 0)
            {
                start = startrooms[_rng.Next(startrooms.Count)] as Room;
                HashSet<Room> usedrooms = new HashSet<Room>();
                Stack<Room> thisDepth = new Stack<Room>();
                Stack<Room> nextDepth = new Stack<Room>();
                usedrooms.Add(start);
                thisDepth.Push(start);
                int depth = 0;

                while (thisDepth.Count > 0)
                {
                    Room room = thisDepth.Pop();
                    foreach (Room connection in room.Connections)
                    {
                        if (!usedrooms.Contains(connection))
                        {
                            nextDepth.Push(connection);
                        }
                    }
                    if (thisDepth.Count == 0 && nextDepth.Count > 0)
                    {
                        thisDepth.Clear();
                        thisDepth = nextDepth;
                        nextDepth = new Stack<Room>();
                        depth++;
                        if (depth > highestDepth)
                        {
                            highestDepth = depth;
                            end = room;
                            start = start;
                        }
                    }
                }
            }
            //now we ought to have a start and a end room
            StartPos = start.Position;
            EndPos = end.Position;
        }

        /// <summary>
        /// Carve a route through the level, removing as little dirt as possible
        /// </summary>
        public void CarveRoute()
        {
            //First select the starting position
            int startX = _rng.Next(5, _gridWidth - 5);
            int startY = _rng.Next(4, 6);
            PlaceStart(startX,startY);
            //pick any point really as the exit
            int endX = _rng.Next(0, _gridWidth);
            int endY = _gridHeight - 1;
            PlaceEnd(endX,endY);
            
            MapKey startKey = new MapKey(startX, startY);
            MapKey endKey = new MapKey(endX, endY);
            //Now start the depth first search
            Func<MapKey, MapKey, double> Heuristic = (a,b) => Math.Sqrt(Math.Pow(b.X-a.X,2)+Math.Pow(b.Y-a.Y,2));
            //Func<MapKey, MapKey, double> Heuristic = (a, b) => (Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y));
            //Func<MapKey, MapKey, double> Heuristic = (a, b) => Math.Sqrt(Math.Abs(b.X - a.X) + Math.Pow(b.Y - a.Y, 2));
            //Dictionary<MapKey, BFSNode> map = new Dictionary<MapKey, BFSNode>();
            //Dictionary<IntPoint, IntPoint> cameFrom = new Dictionary<IntPoint, IntPoint>();
            //Dictionary<IntPoint, double> gScore = new Dictionary<IntPoint, double>();
            //Dictionary<IntPoint, double> fScore = new Dictionary<IntPoint, double>();
            HashSet<MapKey> closedSet = new HashSet<MapKey>();
            HashSet<MapKey> openSet = new HashSet<MapKey>();
            C5.IntervalHeap<BFSNode> openQueue = new C5.IntervalHeap<BFSNode>();

            //setup
            BFSNode startnode = new BFSNode(startKey);
            BFSNode endnode = new BFSNode(endKey);
            startnode.GScore = 0;
            startnode.FScore = Heuristic(startKey, endKey);

            openQueue.Add(startnode);
            openSet.Add(startKey);
            //map.Add(startKey, startnode);
            int check = 0;
            //while (!queue.IsEmpty)
            while (!openQueue.IsEmpty)
            {
                check++;
                BFSNode current = openQueue.DeleteMin();
                System.Console.Out.WriteLine($"check no. {check}... node with pos: [{current.X},{current.Y}] and score: {current.FScore}");
                if (current.Pos.Equals(endKey))
                {
                    endnode = current;
                    break;
                }
                openSet.Remove(current.Pos);
                closedSet.Add(current.Pos);
                
                //add neighbours
                ArrayList neighbours = new ArrayList();
                if (current.X > 0) neighbours.Add(new MapKey(current.X - 1, current.Y));
                if (current.X < GridWidth - 1) neighbours.Add(new MapKey(current.X + 1, current.Y));
                if (current.Y > 0) neighbours.Add(new MapKey(current.X, current.Y - 1));
                if (current.Y < GridHeight - 1) neighbours.Add(new MapKey(current.X, current.Y + 1));
                //
                foreach (MapKey neighbour in neighbours)
                {
                    BFSNode neighbourNode = new BFSNode(neighbour);
                    if (closedSet.Contains(neighbour))
                        continue;
                    /*if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                        //map.Add(neighbour, neighbourNode);
                    }
                    else
                    {
                        //if (!map.TryGetValue(neighbour, out neighbourNode))
                        {
                            //throw new Exception("failed collecting the node from the map");
                        }
                    }*/
                    if (openSet.Contains(neighbour))
                        continue;
                    openSet.Add(neighbour);
                    bool up = neighbour.Y < current.Y;
                    bool down = neighbour.Y > current.Y;
                    bool wall = GetWorldPoint(neighbour.X, neighbour.Y) == 1;
                    double cost = (wall ? down ? 500 : up ? 50 : 200 : down ? 1 : up ? 1 : 1);

                    double tenativeScore = current.GScore + 1 + cost; //+ 2 * GetWorldPoint(neighbour.X, neighbour.Y);
                    
                    //if (tenativeScore >= neighbourNode.GScore)
                    //    continue;
                    neighbourNode.GScore = tenativeScore;
                    neighbourNode.FScore = tenativeScore + Heuristic(neighbour, endKey);
                    neighbourNode.Previous = current;

                    openQueue.Add(neighbourNode);
                }
                neighbours.Clear();
            }
            // now carve the route out
            BFSNode carveNode = endnode;
            while (carveNode != null)
            {
                _world[carveNode.X, carveNode.Y] = 0;
                carveNode = carveNode.Previous;
            }
        }

        #endregion

        #region Private Methods
        private void PlaceStart(int x, int y)
        {
            _startPos = new IntPoint(x, y);
            _world[x - 1, y - 1] = 0;
            _world[x, y - 1] = 0;
            _world[x + 1, y - 1] = 0;
            _world[x - 1, y] = 0;
            _world[x, y] = 0;
            _world[x + 1, y] = 0;
            _world[x - 1, y + 1] = 1;
            _world[x, y + 1] = 1;
            _world[x + 1, y + 1] = 1;
        }
        private void PlaceEnd(int x, int y)
        {
            _endPos = new IntPoint(x, y);
        }

        /// <summary>
        /// Sets up the rooms
        /// </summary>
        private void SetUpRooms()
        {
            Rooms = new ArrayList();
            for (int x = 2; x < GridWidth; x += 5)
            {
                for (int y = 2; y < GridHeight; y += 4)
                {
                    Rooms.Add(new Room(new IntPoint(x, y)));
                }
            }
        }
        /// <summary>
        /// Fills the given 2d array with random integers 1 or 0.
        /// </summary>
        private void RandomizeWorld(int[,] cells)
        {
            double randomNumber;
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    randomNumber = _rng.NextDouble();
                    if (randomNumber > InitPercentage)
                    {
                        cells[x, y] = air;
                    }
                    else
                    {
                        cells[x, y] = wall;
                    }

                }
            }
        }
        /// <summary>
        ///I calculate the integral of the world for calculating the amount of walls at many regions much faster.
        /// </summary>
        private int[,] CalcIntegralWorld(int[,] world)
        {
            int x = 0, y = 0;
            int[,] integral = new int[GridWidth, GridHeight];
            //the corner is calculated first
            integral[x, y] = world[x, y];

            //then i calculate the entire first row
            y = 0;
            for (x = 1; x < GridWidth; x++)
            {
                integral[x, y] = world[x, y] + integral[x - 1, y];
            }
            //and the entire first column
            x = 0;
            for (y = 1; y < GridHeight; y++)
            {
                integral[x, y] = world[x, y] + integral[x, y - 1];
            }
            //now the rest can be calculated more easily.
            for (x = 1; x < GridWidth; x++)
            {
                for (y = 1; y < GridHeight; y++)
                {
                    integral[x, y] = world[x, y] + integral[x - 1, y] + integral[x, y - 1] - integral[x - 1, y - 1];
                }
            }
            return integral;
        }

        /// <summary>
        /// Reset the generator
        /// </summary>
        private void Reset()
        {
            World = null;
            percentage = -1;
            IntegralWorld = null;
            _loners = -1;
        }
        #endregion
    }
    /// <summary>
    /// room used for room identification
    /// </summary>
    public class Room
    {
        public IntPoint Position { get; set; }
        public IntPoint TilesPosition { get; set; }
        public ArrayList Connections {get; private set;}
        public int Tiles { get; set; }
        /// <summary>
        /// room constructor
        /// </summary>
        public Room(IntPoint position)
        {
            Position = position;
            Tiles = 0;
            Connections = new ArrayList();
        }

        public void AddConnection(Room _room)
        {
            Connections.Add(_room);
        }

        public override bool Equals(object obj)
        {
            Room other = obj as Room;
            return (other == null)? false : (other.Position.Equals(Position));
        }
    }
}
