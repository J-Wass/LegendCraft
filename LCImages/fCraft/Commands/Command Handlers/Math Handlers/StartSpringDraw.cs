//Copyright (C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

//Copyright (C) <2012> Lao Tszy (lao_tszy@yahoo.co.uk)

//LegendCraft Edit: Copy and Pasted Handler From 800craft modified for /spring and later similar cmds to follow. This handler was made by 800craft so proper credit shall be given.
//This handler was made to use in those cmds specifically so that no messages would be displayed to the user when doing them.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using fCraft;
using fCraft.Drawing;

namespace fCraft
{
    public class StartSpringDraw : DrawOperation
    {
        private Scaler _scaler2;
        private Expression[] _expressions2;
        private double[][] _paramIterations2;
        private const double MaxIterationSteps2 = 1000000;

        public StartSpringDraw(Player p, Command cmd)
            : base(p)
        {
            _expressions2 = PrepareParametrizedManifold.GetPlayerParametrizationCoordsStorage(p);
            if (null == _expressions2[0])
                throw new InvalidExpressionException("x is undefined");
            if (null == _expressions2[1])
                throw new InvalidExpressionException("y is undefined");
            if (null == _expressions2[2])
                throw new InvalidExpressionException("z is undefined");

            _paramIterations2 = PrepareParametrizedManifold.GetPlayerParametrizationParamsStorage(p);
            if (null == _paramIterations2[0] && null == _paramIterations2[1] && null == _paramIterations2[2])
                throw new InvalidExpressionException("all parametrization variables are undefined");

            if (GetNumOfSteps2(0) * GetNumOfSteps2(1) * GetNumOfSteps2(2) > MaxIterationSteps2)
                throw new InvalidExpressionException("too many iteration steps (over " + MaxIterationSteps2 + ")");

            _scaler2 = new Scaler(cmd.Next());

        }
        public override string Name
        {
            get { return "MathFigure"; }        //What it will say when doing one of these math figure cmds
        }

        public override int DrawBatch(int maxBlocksToDraw)
        {
            int count = 0;
            double fromT, toT, stepT;
            double fromU, toU, stepU;
            double fromV, toV, stepV;

            GetIterationBounds2(0, out fromT, out toT, out stepT);
            GetIterationBounds2(1, out fromU, out toU, out stepU);
            GetIterationBounds2(2, out fromV, out toV, out stepV);

            for (double t = fromT; t <= toT; t += stepT)
            {
                for (double u = fromU; u <= toU; u += stepU)
                {
                    for (double v = fromV; v <= toV; v += stepV)
                    {
                        Coords.X = _scaler2.FromFuncResult(_expressions2[0].Evaluate(t, u, v), Bounds.XMin, Bounds.XMax);
                        Coords.Y = _scaler2.FromFuncResult(_expressions2[1].Evaluate(t, u, v), Bounds.YMin, Bounds.YMax);
                        Coords.Z = _scaler2.FromFuncResult(_expressions2[2].Evaluate(t, u, v), Bounds.ZMin, Bounds.ZMax);
                        if (DrawOneBlock())
                            ++count;
                        //if (TimeToEndBatch)
                        //    return count;
                    }
                }
            }
            IsDone = true;
            return count;
        }

        private double GetNumOfSteps2(int idx)
        {
            if (null == _paramIterations2[idx])
                return 1;
            return (_paramIterations2[idx][1] - _paramIterations2[idx][0]) / _paramIterations2[idx][2] + 1;
        }
        private void GetIterationBounds2(int idx, out double from, out double to, out double step)
        {
            if (null == _paramIterations2[idx])
            {
                from = 0;
                to = 0;
                step = 1;
                return;
            }
            from = _paramIterations2[idx][0];
            to = _paramIterations2[idx][1];
            step = _paramIterations2[idx][2];
        }

        public override bool Prepare(Vector3I[] marks)
        {
            if (!base.Prepare(marks))
            {
                return false;
            }
            BlocksTotalEstimate = Bounds.Volume;
            return true;
        }
    }
}
