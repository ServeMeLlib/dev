int numberInt = int.Parse("100");

for (int i = 1; i < numberInt; i++)
{
  numberInt = numberInt * i;
}
Thread.Sleep(3000);
return numberInt;