# Fam
Transactions overview tool

for DIY and common investors in India

For: **Mutual fund transactions from excel/csv files**

You can download the latest version from here: https://github.com/ManageSoft-79/Fam/releases/tag/v1

Required columns in the files: Date, Folio Number, Name of the Fund, Order, Units, NAV

Supported files:
- CAMS and KFintech transaction statements. Download links below: (Also provided in the app in 'Download Links' section.)
  - https://www.camsonline.com/Investors/Statements/Transaction-Details-Statement
  - https://mfs.kfintech.com/investor/General/InvestorTransactionReport
- Kuvera's Transactions csv export

Full history is required for correct FIFO calculations. Hence request the transaction statements from a date before you started investing.

In Fam load all files of same account together.

## Features
- Transactions overview
- Mutual funds overview
- Categories & subcategories overview
- Capital gains
- Fund wise and capital gain wise transactions
- Categories chart

**Planned features**:
- More transaction filetypes

**Note on Tax categories:**

Tax categories and calculation of capital gains are only tentative. We are not responsible for them. Ascertain the final figures yourself or with the help of your CA.

## Pre-requisites

Requires one time installation of **.NET Desktop Runtime 10.0**

If requested, you can download and install its latest version from here: https://dotnet.microsoft.com/en-us/download/dotnet/10.0

## Development

**Stack:** WPF C# .NET 10.0 (Windows)

*The app is still in development phase. So there could be bugs. You can report bugs in discussions.*