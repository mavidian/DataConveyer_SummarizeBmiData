# DataConveyer_SummarizeBmiData

DataConveyer_SummarizeBmiData is a console application to demonstrate how Data Conveyer can be
used to summarize data.

The process accepts input data in a form of a JSON file containing a list of people along with
their places of residence and some demographic information. Like so:

```json
{
  "Name":"Chucho Charleston",
  "Residence":"GA",
  "BirthDate":"1/19/1954",
  "Gender":"Male",
  "Height":{"ft":5,"in":8},
  "Weight":221
}
```

On the output, the application produces an XML file that summarizes population characteristics
per state. Like so:

```xml
<StateSummaryRecord>
  <State>AK</State>
  <TotalHeadount>12</TotalHeadount>
  <AverageHeight>4'11''</AverageHeight>
  <AverageWeight>204lbs</AverageWeight>
  <AverageBMI>44.2</AverageBMI>
  <OverweightHeadount>10</OverweightHeadount>
  <PercentageOverweight>83%</PercentageOverweight>
</StateSummaryRecord>
```

**Disclaimer 1**: The contents of a sample input file (PeopleDataByState.json located in the Data folder)
has been randomly generated and any possible resemblance to the actual population characteristics
is purely coincidental.

**Disclaimer 2**: Calculation formulas used in this application are approximations used for demonstration
purposes only and should not be used for any other purposes.

## Installation

* Fork this repository and clone it onto your local machine, or

* Download this repository onto your local machine.

## Usage

1. Open DataConveyer_SummarizeBmiData solution in Visual Studio.

2. Build and run the application, e.g. hit F5

    - a console window with directions will show up.

3. Copy an input file (e.g. PeopleDataByState.json from ...Data folder) into the ...Data\In folder

    - the file will get processed as reported in the console window.

4. Review the contents of the output file placed in the ...Data\Out folder.

5. (optional) Repeat steps 3-4 for other additional input file(s).

6. To exit application, hit Enter key into the console window.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License

[Apache License 2.0](https://choosealicense.com/licenses/apache-2.0/)

## Copyright

```
Copyright © 2019 Mavidian Technologies Limited Liability Company. All Rights Reserved.
```
