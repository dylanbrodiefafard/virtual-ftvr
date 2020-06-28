using System.Text;
using Biglab.Calibrations.InteractiveVisual;
using static alglib;

namespace Biglab.Interoperability
{
    public static class ALGLIB
    {
        public struct SolverParameters
        {
            public double[] x0;
            public int n;
            public double[] lowerBounds;
            public double[] upperBounds;
            public double xEpsilon;
            public double maxStep;
            public int maxIterations;
        }

        public static double[] RunSolver(SolverParameters parameters, Optimization.OptimizationVariables variables, out string debugReport)
        {
            minlmstate state;
            minlmcreatevj(parameters.n + 1, parameters.x0, out state);

            minlmsetbc(state, parameters.lowerBounds, parameters.upperBounds);
            minlmsetcond(state, parameters.xEpsilon, parameters.maxIterations);
            minlmsetstpmax(state, parameters.maxStep);
            minlmoptimize(state, VectorFunction, Jacobian, null, variables);

            double[] xn;
            minlmreport report;
            minlmresults(state, out xn, out report);

            debugReport = GetDebugReport(report);

            return xn;
        }

        private static string GetDebugReport(minlmreport report)
        {
            var sBuilder = new StringBuilder();
            sBuilder.AppendLine($"Iterations: {report.iterationscount}");
            switch (report.terminationtype)
            {
                case -8:
                    sBuilder.AppendLine("Termination type: Optimizer detected NAN / INF values either in the function itself, or in its Jacobian");
                    break;
                case -7:
                    sBuilder.AppendLine("Termination type: derivative correctness check failed; see rep.funcidx, rep.varidx for more information");
                    sBuilder.AppendLine($"{nameof(report.funcidx)} : {report.funcidx}, {nameof(report.varidx)} : {report.varidx}");
                    break;
                case -5:
                    sBuilder.AppendLine("Termination type: inappropriate solver was used. solver created with minlmcreatefgh() used  on  problem  with general linear constraints(set with minlmsetlc() call).");
                    break;
                case -3:
                    sBuilder.AppendLine("Termination type: constraints are inconsistent");
                    break;
                case 2:
                    sBuilder.AppendLine("Termination type: relative step is no more than EpsX");
                    break;
                case 5:
                    sBuilder.AppendLine("Termination type: MaxIts steps was taken");
                    break;
                case 7:
                    sBuilder.AppendLine("Termination type: stopping conditions are too stringent, further improvement is impossible.");
                    break;
                case 8:
                    sBuilder.AppendLine("Termination type: terminated   by  user  who  called  MinLMRequestTermination().  X contains point which was \"current accepted\" when termination");
                    break;
                default:
                    sBuilder.AppendLine($"Termination type: unknown ({report.terminationtype})");
                    break;
            }

            return sBuilder.ToString();
        }

        private static void Jacobian(double[] doubles, double[] fi, double[,] jac, object o)
            => Optimization.JacobianMatrix(doubles, fi, jac, (Optimization.OptimizationVariables)o);

        private static void VectorFunction(double[] doubles, double[] fi, object o) 
            => Optimization.VectorFunction(doubles, fi, (Optimization.OptimizationVariables)o);
    }
}
