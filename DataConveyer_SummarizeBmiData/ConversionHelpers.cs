// Copyright © 2019 Mavidian Technologies Limited Liability Company. All Rights Reserved.

using System;

namespace DataConveyer_SummarizeBmiData
{
   /// <summary>
   /// Utility extension methods, such as unit conversions
   /// </summary>
   internal static class ConversionHelpers
   {
      /// <summary>
      /// Convert height in feet and inches into inches
      /// </summary>
      /// <param name="height"></param>
      /// <returns></returns>
      internal static int ToInches(this (int feet, int inches) height)
      {
         return height.feet * 12 + height.inches;
      }

      /// <summary>
      /// Convert inches into height expressed as feet + inches
      /// </summary>
      /// <param name=""></param>
      /// <returns></returns>
      internal static (int feet, int inches) ToHeight(this int inches)
      {
         return (inches / 12, inches % 12);
      }

      /// <summary>
      /// Convert height + weight into BMI
      /// </summary>
      /// <param name="heightAndWeight"></param>
      /// <returns></returns>
      internal static float ToBmi(this (int heightInInches, int weight) heightAndWeight)
      {
         return 703f * heightAndWeight.weight / heightAndWeight.heightInInches / heightAndWeight.heightInInches;
      }

      /// <summary>
      /// Determine if the person with given BMI, age & gender is overweight
      /// </summary>
      /// <param name="bmiAgeGender"></param>
      /// <returns></returns>
      internal static bool IsOverweight(this (float bmi, int age, bool isMale) bmiAgeGender)
      {
         //Rules:
         // Men: 25 plus 0.2 per year between age 20 and 40 and 0.1 per year between age 40 and 60.
         // Women: 24 plus 0.1 per year between age 20 and 60
         float threshold;
         if (bmiAgeGender.isMale)
         {
            if (bmiAgeGender.age < 20) threshold = 25f;
            else if (bmiAgeGender.age < 40) threshold = 25f + .2f * (bmiAgeGender.age - 20);
            else if (bmiAgeGender.age < 60) threshold = 29f + .1f * (bmiAgeGender.age - 40);
            else threshold = 31;
         }
         else
         { //female
            if (bmiAgeGender.age < 20) threshold = 24f;
            else if (bmiAgeGender.age < 60) threshold = 24f + .1f * (bmiAgeGender.age - 20);
            else threshold = 28;
         }
         return bmiAgeGender.bmi > threshold;
      }


      /// <summary>
      /// Convert date of birth to current age
      /// </summary>
      /// <param name="dob"></param>
      /// <returns></returns>
      internal static int ToAge(this DateTime dob)
      {
         //an optional parameter "as of" can be added to calculate age as of dates other than today
         var asOf = DateTime.Now;
         return asOf.Year - dob.Year - (asOf.DayOfYear < dob.DayOfYear ? 1 : 0);
      }
   }
}
