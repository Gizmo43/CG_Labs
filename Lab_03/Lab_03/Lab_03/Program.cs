using Lab_03;


class Program
{
    static void Main(string[] args)
    {
        using (RTX rtx = new RTX(600, 600))
        {
            rtx.Run();
        }
    }
}

