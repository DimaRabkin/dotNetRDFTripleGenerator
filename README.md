dotNetRDFTripleGenerator
========================
Usage:
    
    public class Entity
    {
        [Subject(Prefix = @"http://d/data/")]
        public int Id { get; set; }
        [Object(Predicate = @"http://d/ont/name")]
        public string Name { get; set; }
        [Object(Predicate = @"http://d/ont/priority")]
        public int Priority { get; set; }
        [Object(Predicate = @"http://d/ont/category")]
        public ulong Category { get; set; }
    }
    
    public class HelloWorld
    {
        public static void Main(String[] args)
        {
            IGraph graph = new Graph();
            var generator = new TripleGenerator(new LiteralNodeAdapter(graph));
            var entities = Enumerable.Range(0, 5)
                .Select(ii => new Entity { Id = ii, Category = 52435, Name = string.Format("Entity{0}Name", ii), Priority = ii });
            
            List<Triple> triples = entities.SelectMany(generator.GenerateTriples).ToList();
            graph.Assert(triples);

            var store = new TripleStore();
            store.Add(graph);

            string query = File.ReadAllText("query.txt");
            var executeQuery = graph.ExecuteQuery(query);

        }
    }

Results:

http://d/data/0 , http://d/ont/name , Entity0Name^^http://www.w3.org/2001/XMLSchema#string

http://d/data/0 , http://d/ont/priority , 0^^http://www.w3.org/2001/XMLSchema#integer

http://d/data/0 , http://d/ont/category , 52435^^http://www.w3.org/2001/XMLSchema#unsignedLong

http://d/data/1 , http://d/ont/name , Entity1Name^^http://www.w3.org/2001/XMLSchema#string

http://d/data/1 , http://d/ont/priority , 1^^http://www.w3.org/2001/XMLSchema#integer

http://d/data/1 , http://d/ont/category , 52435^^http://www.w3.org/2001/XMLSchema#unsignedLong

http://d/data/2 , http://d/ont/name , Entity2Name^^http://www.w3.org/2001/XMLSchema#string

http://d/data/2 , http://d/ont/priority , 2^^http://www.w3.org/2001/XMLSchema#integer

http://d/data/2 , http://d/ont/category , 52435^^http://www.w3.org/2001/XMLSchema#unsignedLong

http://d/data/3 , http://d/ont/name , Entity3Name^^http://www.w3.org/2001/XMLSchema#string

http://d/data/3 , http://d/ont/priority , 3^^http://www.w3.org/2001/XMLSchema#integer

http://d/data/3 , http://d/ont/category , 52435^^http://www.w3.org/2001/XMLSchema#unsignedLong

http://d/data/4 , http://d/ont/name , Entity4Name^^http://www.w3.org/2001/XMLSchema#string

http://d/data/4 , http://d/ont/priority , 4^^http://www.w3.org/2001/XMLSchema#integer

http://d/data/4 , http://d/ont/category , 52435^^http://www.w3.org/2001/XMLSchema#unsignedLong
