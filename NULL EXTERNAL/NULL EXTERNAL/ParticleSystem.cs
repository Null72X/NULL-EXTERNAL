using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;

namespace INTERNAL_LOADER
{
    public class ParticleSystem
    {
        private readonly Control target;

        private readonly System.Windows.Forms.Timer timer;

        private readonly List<Particle> particles =
            new List<Particle>();

        private readonly Random random =
            new Random();

        private bool paused = false;

        // SWEET SPOT
        private const int ParticleCount = 75;

        public ParticleSystem(Control control)
        {
            target = control;

            EnableDoubleBuffer(target);

            CreateParticles();

            timer =
                new System.Windows.Forms.Timer();

            // SMOOTHER + SLIGHTLY FASTER
            timer.Interval = 14;

            timer.Tick += Update;

            timer.Start();

            target.Paint += Draw;

            target.Resize += (s, e) =>
            {
                particles.Clear();

                CreateParticles();
            };
        }

        // ==========================================
        // CREATE
        // ==========================================

        private void CreateParticles()
        {
            for (int i = 0; i < ParticleCount; i++)
            {
                particles.Add(
                    new Particle(
                        random,
                        target.Width,
                        target.Height));
            }
        }

        // ==========================================
        // UPDATE
        // ==========================================

        private void Update(
            object sender,
            EventArgs e)
        {
            if (paused)
                return;

            foreach (var p in particles)
            {
                p.Update(
                    target.Width,
                    target.Height);
            }

            target.Invalidate();
        }

        // ==========================================
        // DRAW
        // ==========================================

        private void Draw(
            object sender,
            PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.SmoothingMode =
                SmoothingMode.HighQuality;

            DrawConnections(g);

            foreach (var p in particles)
            {
                p.Draw(g);
            }
        }

        // ==========================================
        // CONNECTIONS
        // ==========================================

        private void DrawConnections(Graphics g)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                for (int j = i + 1; j < particles.Count; j++)
                {
                    float dx =
                        particles[i].X - particles[j].X;

                    float dy =
                        particles[i].Y - particles[j].Y;

                    float distance =
                        (float)Math.Sqrt(dx * dx + dy * dy);

                    // BETTER CONNECTION RANGE

                    if (distance < 130)
                    {
                        int alpha =
                            (int)(28 - distance / 5);

                        if (alpha < 0)
                            alpha = 0;

                        using (Pen pen =
                               new Pen(
                                   Color.FromArgb(
                                       alpha,
                                       170,
                                       0,
                                       0),
                                   1f))
                        {
                            g.DrawLine(
                                pen,
                                particles[i].X,
                                particles[i].Y,
                                particles[j].X,
                                particles[j].Y);
                        }
                    }
                }
            }
        }

        // ==========================================
        // CONTROLS
        // ==========================================

        public void Pause()
        {
            paused = true;
        }

        public void Resume()
        {
            paused = false;
        }

        public void Toggle()
        {
            paused = !paused;
        }

        // ==========================================
        // DOUBLE BUFFER
        // ==========================================

        private void EnableDoubleBuffer(
            Control control)
        {
            typeof(Control)
                .GetProperty(
                    "DoubleBuffered",
                    BindingFlags.NonPublic |
                    BindingFlags.Instance)
                ?.SetValue(control, true, null);
        }
    }

    // ==========================================
    // PARTICLE
    // ==========================================

    public class Particle
    {
        private float x;
        private float y;

        private float velocityX;
        private float velocityY;

        private float size;

        private float rotation;
        private float rotationSpeed;

        private readonly Color color;

        public float X => x;
        public float Y => y;

        public Particle(
            Random random,
            int width,
            int height)
        {
            x = random.Next(width);

            y = random.Next(height);

            // BETTER SIZE RANGE

            size =
                random.Next(2, 12);

            // SLIGHTLY FASTER FLOAT

            velocityX =
                (float)(random.NextDouble() - 0.5f) * 0.35f;

            velocityY =
                (float)(random.NextDouble() - 0.5f) * 0.35f;

            rotation =
                random.Next(360);

            rotationSpeed =
                (float)(random.NextDouble() - 0.5f) * 0.35f;

            // PREMIUM RED SHADES

            Color[] colors =
            {
                Color.FromArgb(120,0,0),
                Color.FromArgb(180,0,0),
                Color.FromArgb(200,20,20),
                Color.FromArgb(150,10,10),
                Color.FromArgb(220,30,30)
            };

            color =
                colors[random.Next(colors.Length)];
        }

        // ==========================================
        // UPDATE
        // ==========================================

        public void Update(
            int width,
            int height)
        {
            x += velocityX;

            y += velocityY;

            rotation += rotationSpeed;

            // NATURAL FLOATING

            velocityX +=
                (float)(Math.Sin(rotation * 0.015f) * 0.0012f);

            velocityY +=
                (float)(Math.Cos(rotation * 0.015f) * 0.0012f);

            // LIMIT SPEED

            velocityX =
                Math.Max(
                    -0.45f,
                    Math.Min(0.45f, velocityX));

            velocityY =
                Math.Max(
                    -0.45f,
                    Math.Min(0.45f, velocityY));

            // SCREEN WRAP

            if (x < -40)
                x = width + 40;

            if (x > width + 40)
                x = -40;

            if (y < -40)
                y = height + 40;

            if (y > height + 40)
                y = -40;
        }

        // ==========================================
        // DRAW
        // ==========================================

        public void Draw(Graphics g)
        {
            GraphicsState state =
                g.Save();

            g.TranslateTransform(x, y);

            g.RotateTransform(rotation);

            PointF[] triangle =
            {
                new PointF(0, -size),

                new PointF(-size, size),

                new PointF(size, size)
            };

            // OUTER GLOW

            using (SolidBrush glow =
                   new SolidBrush(
                       Color.FromArgb(
                           18,
                           255,
                           0,
                           0)))
            {
                g.FillPolygon(glow, triangle);
            }

            // INNER GLOW

            using (SolidBrush glow2 =
                   new SolidBrush(
                       Color.FromArgb(
                           12,
                           255,
                           40,
                           40)))
            {
                g.FillPolygon(glow2, triangle);
            }

            // MAIN PARTICLE

            using (SolidBrush brush =
                   new SolidBrush(color))
            {
                g.FillPolygon(brush, triangle);
            }

            g.Restore(state);
        }
    }
}