namespace ImageTracerNet.Vectorization.Points
{
    internal class InterpolationPoint : Point<double>
    {
        public Heading Direction { get; set; } = Heading.Center;
    }
}
