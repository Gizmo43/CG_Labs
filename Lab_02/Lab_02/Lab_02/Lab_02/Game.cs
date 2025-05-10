using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
using static Lab_02.Game;

namespace Lab_02
{
    internal class Game : GameWindow
    {
        private int width, height;
        private float boatPosition = 0; // -1, 0, 1 - три линии движения
        private float gameSpeed = 1.5f;
        private float boatSpeed = 1.0f;
        private bool gameOver = false;
        private Random random = new Random();

        // Катер
        private Boat boat;

        // Бревна
        private List<Log> logs = new List<Log>();
        private float logSpawnTimer = 0;
        private float logSpawnInterval = 2.0f;

        // Окружение
        private River river;
        private Bank leftBank;
        private Bank rightBank;
        private Skybox skybox;

        public Game(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.CenterWindow(new Vector2i(width, height));
            this.width = width;
            this.height = height;
        }

        protected override void OnLoad()
        {
            GL.Enable(EnableCap.DepthTest);

            // Инициализация объектов
            skybox = new Skybox();
            boat = new Boat();
            river = new River();
            leftBank = new Bank(-1.7f); 
            rightBank = new Bank(1.7f);  

            base.OnLoad();
        }

        protected override void OnUnload()
        {
            boat.OnUnload();
            river.OnUnload();
            leftBank.OnUnload();
            rightBank.OnUnload();
            skybox.OnUnload();

            foreach (var log in logs)
            {
                log.OnUnload();
            }

            base.OnUnload();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Камера сверху, смотрящая по диагонали вниз
            Vector3 cameraPosition = new Vector3(0f, 3f, 3f);  
            Vector3 cameraTarget = new Vector3(0f, 0f, -2f);  
            Vector3 cameraDown = Vector3.UnitY;                  

            Matrix4 view = Matrix4.LookAt(cameraPosition, cameraTarget, cameraDown);         // Вектор вверх

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60.0f),
                (float)width / height, 0.1f, 100.0f);

            // Отрисовка окружения
            skybox.Draw(view, projection);
            river.Draw(Matrix4.Identity, view, projection);
            leftBank.Draw(Matrix4.Identity, view, projection);
            rightBank.Draw(Matrix4.Identity, view, projection);

            // Отрисовка бревен
            foreach (var log in logs)
            {
                log.Draw(view, projection);
            }

            // Отрисовка катера
            Matrix4 boatModel = Matrix4.CreateTranslation(boatPosition, 0f, -1.5f);
            boat.Draw(boatModel, view, projection);

            
            Context.SwapBuffers();
            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            if (gameOver)
            {
                Close();
            }

            // Управление катером
            if (KeyboardState.IsKeyDown(Keys.Left))
            {
                boatPosition = MathHelper.Clamp(boatPosition - boatSpeed * (float)args.Time, -1f, 1f);
            }
            if (KeyboardState.IsKeyDown(Keys.Right))
            {
                boatPosition = MathHelper.Clamp(boatPosition + boatSpeed * (float)args.Time, -1f, 1f);
            }

            // Обновление бревен
            logSpawnTimer += (float)args.Time;
            if (logSpawnTimer >= logSpawnInterval)
            {
                logSpawnTimer = 0;
                gameSpeed += 0.1f;
                boatSpeed += 0.1f;
                logSpawnInterval = MathHelper.Clamp(logSpawnInterval - 0.1f, 0.3f, 2.0f);
                SpawnLog();
            }

            // Движение бревен 
            for (int i = logs.Count - 1; i >= 0; i--)
            {
                logs[i].Move(gameSpeed * (float)args.Time);

                // Проверка столкновений
                if (Math.Abs(logs[i].PositionX - boatPosition) < 0.3f &&
                    Math.Abs(logs[i].PositionZ - (-1.5f)) < 0.3f) {
                    gameOver = true; //остановка
                }

                // Удаление бревен за экраном
                if (logs[i].PositionZ > 3f)
                {
                    logs[i].OnUnload();
                    logs.RemoveAt(i);
                }
            }

            base.OnUpdateFrame(args);
        }

        private void SpawnLog()
        {
            float[] lanes = { -1f, 0f, 1f };
            float lane = lanes[random.Next(3)];
            logs.Add(new Log(lane, -14f));
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            this.width = e.Width;
            this.height = e.Height;
        }

        // Базовый класс
        public abstract class GameObject
        {
            protected int VAO, VBO, EBO, textureVBO;
            protected int textureID;
            protected Shader shader;
            protected List<Vector3> vertices;
            protected List<Vector2> texCoords;
            protected uint[] indices;

            protected string texPath = "../../../Textures/image.jpg";

            protected void Initialize()
            {
                // Инициализация шейдера
                shader = new Shader();
                shader.LoadShader();

                // Инициализация буферов
                VAO = GL.GenVertexArray();
                VBO = GL.GenBuffer();
                EBO = GL.GenBuffer();

                GL.BindVertexArray(VAO);

                // Вершины
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
                GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * Vector3.SizeInBytes,
                             vertices.ToArray(), BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(0);

                // Текстурные координаты
                textureVBO = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Count * Vector2.SizeInBytes,
                             texCoords.ToArray(), BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(1);

                // Индексы
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint),
                             indices, BufferUsageHint.StaticDraw);

                // Текстура
                textureID = GL.GenTexture();
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, textureID);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

                // Загрузка текстуры
                StbImage.stbi_set_flip_vertically_on_load(1);
                ImageResult texture = ImageResult.FromStream(File.OpenRead(texPath),
                ColorComponents.RedGreenBlueAlpha);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                              texture.Width, texture.Height, 0, PixelFormat.Rgba,
                              PixelType.UnsignedByte, texture.Data);

                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.BindVertexArray(0);
            }

            public void Draw(Matrix4 model, Matrix4 view, Matrix4 projection)
            {
                shader.UseShader();

                // Передача в шейдер
                int modelLoc = GL.GetUniformLocation(shader.shaderHandle, "model");
                int viewLoc = GL.GetUniformLocation(shader.shaderHandle, "view");
                int projectionLoc = GL.GetUniformLocation(shader.shaderHandle, "projection");

                GL.UniformMatrix4(modelLoc, true, ref model);
                GL.UniformMatrix4(viewLoc, true, ref view);
                GL.UniformMatrix4(projectionLoc, true, ref projection);

                // Активация текстуры
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, textureID);

                // Отрисовка
                GL.BindVertexArray(VAO);
                GL.DrawElements(PrimitiveType.Triangles, indices.Length,
                               DrawElementsType.UnsignedInt, 0);
                GL.BindVertexArray(0);
            }


            public void OnUnload()
            {
                GL.DeleteBuffer(VAO);
                GL.DeleteBuffer(VBO);
                GL.DeleteBuffer(EBO);
                shader.DeleteShader();
                GL.DeleteTexture(textureID);
            }
        }

        // Класс катера
        public class Boat : GameObject
        {
            public Boat() : base()
            {
                
                vertices = new List<Vector3>{
                    // Корпус
                    new Vector3(-0.3f, 0.2f, 0.5f),
                    new Vector3(0.3f, 0.2f, 0.5f),
                    new Vector3(0.3f, 0.2f, -0.5f),
                    new Vector3(-0.3f, 0.2f, -0.5f),
                
                    // Нос
                    new Vector3(-0.3f, 0.4f, 0.0f),
                    new Vector3(0.3f, 0.4f, 0.0f),
                };

                indices = new uint[]{
                    // Корпус
                    0, 1, 2, 2, 3, 0,
                    // Борта
                    0, 3, 4,
                    1, 2, 5,
                    // Нос
                    4, 5, 2,
                    4, 2, 3
                };


                texCoords = new List<Vector2>{
                    new Vector2(0, 1), new Vector2(1, 1),
                    new Vector2(1, 0), new Vector2(0, 0),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)
                };

                texPath = "../../../Textures/boat_tex.jpg";

                Initialize();
            }
        }

        // Класс бревна
        public class Log : GameObject
        {
            public float PositionX { get; private set; }
            public float PositionZ { get; private set; }

            public Log(float x, float z) : base()
            {
                PositionX = x;
                PositionZ = z;

                // Вершины для бревна (цилиндр)
                vertices = new List<Vector3>();
                texCoords = new List<Vector2>();
                texPath = "../../../Textures/log_tex.jpg";

                int segments = 8;
                float radius = 0.2f;
                float length = 0.8f;

                // Боковая поверхность
                for (int i = 0; i < segments; i++)
                {
                    float angle1 = i * 2 * MathHelper.Pi / segments;
                    float angle2 = (i + 1) * 2 * MathHelper.Pi / segments;

                    // Вершины для боковой поверхности
                    vertices.Add(new Vector3(radius * (float)Math.Cos(angle1), radius * (float)Math.Sin(angle1), length / 2));
                    vertices.Add(new Vector3(radius * (float)Math.Cos(angle2), radius * (float)Math.Sin(angle2), length / 2));
                    vertices.Add(new Vector3(radius * (float)Math.Cos(angle2), radius * (float)Math.Sin(angle2), -length / 2));
                    vertices.Add(new Vector3(radius * (float)Math.Cos(angle1), radius * (float)Math.Sin(angle1), -length / 2));

                    // Текстурные координаты
                    texCoords.Add(new Vector2(i / (float)segments, 1));
                    texCoords.Add(new Vector2((i + 1) / (float)segments, 1));
                    texCoords.Add(new Vector2((i + 1) / (float)segments, 0));
                    texCoords.Add(new Vector2(i / (float)segments, 0));
                }

                // Индексы для боковой поверхности
                indices = new uint[segments * 6];
                for (int i = 0; i < segments; i++)
                {
                    indices[i * 6] = (uint)(i * 4);
                    indices[i * 6 + 1] = (uint)(i * 4 + 1);
                    indices[i * 6 + 2] = (uint)(i * 4 + 2);
                    indices[i * 6 + 3] = (uint)(i * 4);
                    indices[i * 6 + 4] = (uint)(i * 4 + 2);
                    indices[i * 6 + 5] = (uint)(i * 4 + 3);
                }

                Initialize();
            }

            public void Move(float distance)
            {
                PositionZ += distance;
            }

            public void Draw(Matrix4 view, Matrix4 projection)
            {
                Matrix4 model = Matrix4.CreateTranslation(PositionX, 0.1f, PositionZ);
                base.Draw(model, view, projection);
            }
        }

        // Класс реки
        public class River : GameObject
        {
            public River() : base()
            {
                // Простая плоскость для реки
                vertices = new List<Vector3>{
                    new Vector3(-1.2f, 0.0f, 15f),
                    new Vector3(1.2f, 0.0f, 15f),
                    new Vector3(1.2f, 0.0f, -15f),
                    new Vector3(-1.2f, 0.0f, -15f)
                };

                indices = new uint[] { 0, 1, 2, 2, 3, 0 };
                texPath = "../../../Textures/water_tex.jpg";


                texCoords = new List<Vector2>{
                    new Vector2(0, 10),
                    new Vector2(2, 10),
                    new Vector2(2, 0),
                    new Vector2(0, 0)
                };

                Initialize();
            }
        }

        // Класс берега
        public class Bank : GameObject
        {
            public Bank(float xPosition) : base()
            {

                if (xPosition < 0)
                {
                    vertices = new List<Vector3>{
                        new Vector3(xPosition - 0.5f, 1.0f, 15f),
                        new Vector3(xPosition + 0.5f, 0.0f, 15f),
                        new Vector3(xPosition + 0.5f, 0.0f, -15f),
                        new Vector3(xPosition - 0.5f, 1.0f, -15f)
                    };

                }
                else
                {
                    vertices = new List<Vector3>{
                        new Vector3(xPosition - 0.5f, 0.0f, 15f),
                        new Vector3(xPosition + 0.5f, 1.0f, 15f),
                        new Vector3(xPosition + 0.5f, 1.0f, -15f),
                        new Vector3(xPosition - 0.5f, 0.0f, -15f)
                    };

                }

                indices = new uint[] { 0, 1, 2, 2, 3, 0 };

                texCoords = new List<Vector2>{
                    new Vector2(0, 10),
                    new Vector2(1, 10),
                    new Vector2(1, 0),
                    new Vector2(0, 0)
                };
                texPath = "../../../Textures/bank_tex.jpeg";

                Initialize();
            }
        }

        public class Skybox : GameObject
        {
            public Skybox() : base()
            {
                texPath = "../../../Textures/skybox.jpg";

                vertices = new List<Vector3> {
                    // Передняя грань
                    new Vector3(-50.0f, -50.0f, -50.0f),
                    new Vector3( 50.0f, -50.0f, -50.0f),
                    new Vector3( 50.0f,  50.0f, -50.0f),
                    new Vector3(-50.0f,  50.0f, -50.0f),
            
                    // Задняя грань
                    new Vector3(-50.0f, -50.0f, 50.0f),
                    new Vector3( 50.0f, -50.0f, 50.0f),
                    new Vector3( 50.0f,  50.0f, 50.0f),
                    new Vector3(-50.0f,  50.0f, 50.0f)
                };

                indices = new uint[]{
                    // Передняя грань
                    0, 1, 2, 2, 3, 0,
                    // Задняя грань
                    4, 5, 6, 6, 7, 4,
                    // Левая грань
                    0, 3, 7, 7, 4, 0,
                    // Правая грань
                    1, 2, 6, 6, 5, 1,
                    // Верхняя грань
                    3, 2, 6, 6, 7, 3,
                    // Нижняя грань
                    0, 1, 5, 5, 4, 0
                };

               
                texCoords = new List<Vector2>{
                    // Передняя грань
                    new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f),
                    new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f),
            
                    // Задняя грань
                    new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f),
                    new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f)
                };

                Initialize();
            }

            public void Draw(Matrix4 view, Matrix4 projection)
            {
                view.ClearTranslation();

                GL.Disable(EnableCap.DepthTest);
                base.Draw(Matrix4.Identity, view, projection);
                GL.Enable(EnableCap.DepthTest);
            }
        }

        

        public class Shader
        {
            public int shaderHandle;

            public void LoadShader()
            {
                shaderHandle = GL.CreateProgram();
                int vertexShader = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(vertexShader, LoadShaderSource("shader.vert"));

                GL.CompileShader(vertexShader);
                GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success1);
                if (success1 == 0)
                {
                    string infoLog = GL.GetShaderInfoLog(vertexShader);
                    Console.WriteLine(infoLog);
                }

                int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(fragmentShader, LoadShaderSource("shader.frag"));

                GL.CompileShader(fragmentShader);
                GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int success2);
                if (success2 == 0)
                {
                    string infoLog = GL.GetShaderInfoLog(fragmentShader);
                    Console.WriteLine(infoLog);
                }

                GL.AttachShader(shaderHandle, vertexShader);
                GL.AttachShader(shaderHandle, fragmentShader);
                GL.LinkProgram(shaderHandle);
            }

            public static string LoadShaderSource(string filepath)
            {
                string shaderSource = "";
                try
                {
                    using (StreamReader reader = new StreamReader("../../../Shaders/" + filepath))
                    {
                        shaderSource = reader.ReadToEnd();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to load shader source file:" + e.Message);
                }
                return shaderSource;
            }

            public void UseShader()
            {
                GL.UseProgram(shaderHandle);
            }

            public void DeleteShader()
            {
                GL.DeleteProgram(shaderHandle);
            }
        }
    }
}
    
