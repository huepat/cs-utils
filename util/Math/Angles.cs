namespace HuePat.Util.Math {
    public static class Angles {
        private const double FULL_CIRCLE = 2 * System.Math.PI;

        public static double SubstractDegree(
                double angle1, 
                double angle2) {

            return 
                Substract(
                    angle1.DegreeToRadian(),
                    angle2.DegreeToRadian())
                .RadianToDegree();
        }

        public static double Substract(
                double angle1, 
                double angle2) {

            double result1 = System.Math.Max(angle1, angle2) - System.Math.Min(angle1, angle2);
            double result2 = result1 - FULL_CIRCLE;

            if (result2.Abs() < result1.Abs()) {

                if (angle1 < angle2) {
                    angle1 = angle1 + FULL_CIRCLE;
                }
                else {
                    angle2 = angle2 + FULL_CIRCLE;
                }
            }

            return angle1 - angle2;
        }
    }
}