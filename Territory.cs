using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace GraphicalRisk
{
    public class Territory
    {
        public string name;
        public string continent;
        public string player;
        public int armies;
        public List<Territory> connections;
        public Polygon area;
        public TextBox text;

        public bool IsPointInside(Point testPoint)
        {
            List<Point> polygon = this.area.points;
            bool result = false;
            int j = polygon.Count() - 1;
            for (int i = 0; i < polygon.Count(); i++)
            {
                // For each line segment of the polygon, see if the y value in question is between the y values of the two endpoints
                if (polygon[i].y < testPoint.y && polygon[j].y >= testPoint.y || 
                    polygon[j].y < testPoint.y && polygon[i].y >= testPoint.y)
                {
                    // I can not wrap my head around why this works. I found this code online, iirc.
                    if (polygon[i].x + (testPoint.y - polygon[i].y) / (polygon[j].y - 
                        polygon[i].y) * (polygon[j].x - polygon[i].x) < testPoint.x)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

        public void Build(int x, int y)
        {
            area.points.Add(new Point(x, y));
        }

        public Territory(string nameI, string continentI)
        {
            this.name = nameI;
            this.continent = continentI;
            player = "Neutral";
            armies = 0;
            connections = new List<Territory>();
            area = new Polygon();
        }

        public void Conquer(string playerI, int armiesI, Color colorI)
        {
            this.player = playerI;
            if (armiesI != -1)
                this.armies = armiesI;
            if (null != this.text)
            {
                this.text.Text = "" + armiesI;
                this.text.BackColor = colorI;
            }
        }

        public void Draft(int newArmies)
        {
            this.armies += newArmies;
            this.text.Text = "" + this.armies;
        }

        public bool Fortify(int amount, Territory destination)
        {
            if (!connections.Contains(destination))
                return false;
            if (armies == 1 || amount >= armies || amount < 1)
                return false;
            if (amount < armies)
            {
                armies -= amount;
                destination.Draft(amount);
                return true;
            }
            Console.WriteLine("Something weird happened in Fortify()");
            return false;
        }

        public static void Connect(Territory one, Territory two)
        {
            one.connections.Add(two);
            two.connections.Add(one);
        }

        public void Bind(TextBox bindMe)
        {
            text = bindMe;
        }
    }
}
