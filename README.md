# fluent-migrator-autogen

A Sample Console Application which allows you to point at a directory of SQL scripts and automatically bundle them into an assembly for running using FluentMigrator

To test run

Generate an assembly based on a directory of scripts
.\FluentMigratorRD.exe -m "GeneratePackage" -s "C:\Temp\SomeDirectoryOfScripts\" -o C:\Temp\autogen.dll


Executing those scripts
.\FluentMigratorRD.exe -m "ExecutePackage" -i C:\Temp\autogen.dll -c "server=.;Trusted_Connection=yes;database=dbname;Integrated Security=SSPI;"