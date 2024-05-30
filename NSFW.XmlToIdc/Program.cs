// This is an open source non-commercial project. Dear PVS-Studio, please check it.

// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.XPath;
using System.CommandLine;

namespace NSFW.XmlToIdc
{

	enum CliCommand
	{
		none = -1,
		help,
		tables,
		stdparam,
		extparam,
		makeall,
		ecuf,
		scoobyrom
	}

internal class Program
{
	/// <summary>
	/// Custom exception after which program must terminate.
	/// </summary>
	private class FatalException : ApplicationException
	{
		public FatalException() : base() {}
		public FatalException(string message) : base(message) {}
		public FatalException(string message, Exception inner) : base(message, inner) {}
	}
	private static string _all_defs_dir = "";
	private static string all_defs_dir
	{
		get => _all_defs_dir;
		set => _all_defs_dir = ValidDirString(value);
	}

	private static string _ecu_defs = "ecu_defs.xml";
	private static string ecu_defs
	{
		get => all_defs_dir + _ecu_defs;
		set => _ecu_defs = value;
	}

	private static string _logger_dir = "";
	private static string logger_dir
	{
		get => _logger_dir;
		set => _logger_dir = ValidDirString(value);
	}
	
	private static string _logger_xml = "logger.xml";
	private static string logger_xml
	{
		get => LoggerFileFullPath(_logger_xml);
		set => _logger_xml = value;
	}
	
	private static string _logger_dtd = "logger.dtd";
	private static string logger_dtd
	{
		get => LoggerFileFullPath(_logger_dtd);
		set => _logger_dtd = value;
	}

	private static CliCommand cmd = CliCommand.none;

	private static HashSet<string> names = new HashSet<string>();

	private static IDictionary<string, string> tableList = new Dictionary<string, string>();

	/// <summary>
	/// Returns full path with name to logger file.
	/// Path to logger files is overriden by
	/// path to all files if set.
	/// </summary>
	private static string LoggerFileFullPath(string loggerFilename)
	{
		string dirName = String.IsNullOrEmpty(all_defs_dir) ? logger_dir : all_defs_dir;
		return dirName + loggerFilename;
	}
	/// <summary>
	/// Convert hex number as string to integer
	/// </summary>
	private static bool HexTryParse(string hexString, out Int32? number)
	{
		number = null;
		bool result = false;

		try
		{
			number = Convert.ToInt32(hexString, 16);
			result = true;
		}
		catch {}

		return result;
	}

	private static bool HexTryParse(string hexString, out UInt32? number)
	{
		number = null;
		bool result = false;

		try
		{
			number = Convert.ToUInt32(hexString, 16);
			result = true;
		}
		catch {}

		return result;
	}

	/// <summary>
	/// Adds backslash to path if not present
	/// I.e. 'c:\temp' => 'c:\temp\'
	/// </summary>
	private static string ValidDirString (string value)
	{
		if (String.IsNullOrEmpty(value) || value.EndsWith("\\"))
					return value;
				else
					return value + "\\";
	}

	private static void SetDirsAndPaths (string cli_all_defs_dir,
										string cli_ecu_defs,
										string cli_logger_xml,
										string cli_logger_dir,
										string cli_logger_dtd)
	{
		all_defs_dir	= cli_all_defs_dir;
		ecu_defs		= cli_ecu_defs;
		logger_xml		= cli_logger_xml;
		logger_dir		= cli_logger_dir;
		logger_dtd		= cli_logger_dtd;
	}

	private static string StringToUpperSafe(string s)
	{
		return String.IsNullOrEmpty(s) ? "" : s.ToUpper();
	}
	private static void Main(string[] args)
	{
		string	calId			= "",
				cpu				= "",
				target			= "",
				ecuId			= "",
				ssmBaseString	= "",
				filename_xml	= "";
		bool keepCalIdSymbolCase = false;

		void SetVariablesFromCli(string cli_cal_id = "",
								string	cli_cpu = "",
								string	cli_target = "",
								string	cli_ecu_id = "",
								string	cli_ssmBaseString = "",
								string	cli_filename_xml = "",
								string	cli_all_defs_dir = "",
								string	cli_ecu_defs = "",
								string	cli_logger_xml = "",
								string	cli_logger_dir = "",
								string	cli_logger_dtd = "",
								bool	cli_keep_cal_id_symbol_case = false)
		{
			SetDirsAndPaths(cli_all_defs_dir: cli_all_defs_dir,
							cli_ecu_defs: cli_ecu_defs,
							cli_logger_xml: cli_logger_xml,
							cli_logger_dir: cli_logger_dir,
							cli_logger_dtd: cli_logger_dtd);
			
			keepCalIdSymbolCase = cli_keep_cal_id_symbol_case;
			calId		  = keepCalIdSymbolCase ? cli_cal_id : StringToUpperSafe(cli_cal_id);
			cpu			  = cli_cpu;
			target		  = StringToUpperSafe(cli_target);
			ecuId		  = StringToUpperSafe(cli_ecu_id);
			ssmBaseString = StringToUpperSafe(cli_ssmBaseString);
			filename_xml  = StringToUpperSafe(cli_filename_xml);
		}

		var cli_root = new RootCommand("Convert ECU and logger definitions to IDC script.");

		var cli_all_defs_dir = new Option<string>
		(
			name: "--all-defs-dir",
			description: "Directory where ECU defs, logger defs and .dtd file are placed."
		);
		cli_all_defs_dir.AddAlias("-a");
		cli_root.AddGlobalOption(cli_all_defs_dir);

		var cli_logger_dir = new Option<string>
		(
			name: "--logger-dir",
			description: "Directory where logger defs and .dtd file are placed."
		);
		cli_logger_dir.AddAlias("-l");
		cli_root.AddGlobalOption(cli_logger_dir);

		var cli_ecu_defs = new Option<string>
		(
			name: "--ecu-defs",
			description: "ECU definitions file name.",
			getDefaultValue: () => "ecu_defs.xml"
		);
		cli_ecu_defs.AddAlias("-e");
		cli_root.AddGlobalOption(cli_ecu_defs);

		var cli_logger_xml = new Option<string>
		(
			name: "--logger-defs",
			description: "Logger definitions file name.",
			getDefaultValue: () => "logger.xml"
		);
		cli_logger_xml.AddAlias("-g");
		cli_root.AddGlobalOption(cli_logger_xml);

		var cli_logger_dtd = new Option<string>
		(
			name: "--logger-dtd",
			description: "Logger dtd file name.",
			getDefaultValue: () => "logger.dtd"
		);
		cli_logger_dtd.AddAlias("-d");
		cli_root.AddGlobalOption(cli_logger_dtd);

		var cli_keep_cal_id_symbol_case = new Option<bool>
		(
			name: "--keep-cal-id-symbol-case",
			description: "Keep CAL ID symbol case, do not transform to uppercase"
		);
		cli_root.AddGlobalOption(cli_keep_cal_id_symbol_case);

		//If no commands specified, print help
		cli_root.SetHandler(() =>
							{
								cmd = CliCommand.help;
							}
		);

		var cli_cal_id = new Argument<string>(
			name: "cal-id",
			description: "Calibration id, e.g. A2WC522N"
		);

		var cli_cpu = new Argument<string>(
			name: "cpu",
			description: "CPU bits identifier of the ECU, e.g. 16 or 32"
		);
		
		var cli_target = new Argument<string>(
			name: "target",
			description: "Car control module, e.g. ecu (engine control unit) or tcu (transmission control unit)"
		);
		
		var cli_ecu_id = new Argument<string>(
			name: "ecu-id",
			description: "ECU identifier, e.g. 2F12785606"
		);
		
		var cli_ssmBaseString = new Argument<string>(
			name: "ssm-base",
			description: "Base address of the SSM 'read' vector, e.g. 4EDDC"
		);
		
		var cli_filename_xml = new Argument<string>(
			name: "filename.xml"
		);
		
		var cli_tables = new Command(
			name: "tables",
			description: "Convert tables only."
		);

		cli_tables.AddAlias("t");
		cli_tables.AddArgument(cli_cal_id);
		SetHandler(cli_tables, CliCommand.tables);
		cli_root.AddCommand(cli_tables);

		var cli_stdparam = new Command(
			name: "stdparam",
			description: "Convert standard parameters only."
		);

		cli_stdparam.AddAlias("s");
		cli_stdparam.AddArgument(cli_cpu);
		cli_stdparam.AddArgument(cli_target);
		cli_stdparam.AddArgument(cli_cal_id);
		cli_stdparam.AddArgument(cli_ssmBaseString);
		SetHandler(cli_stdparam, CliCommand.stdparam);
		cli_root.AddCommand(cli_stdparam);

		var cli_extparam = new Command(
			name: "extparam",
			description: "Convert extended parameters only."
		);

		cli_extparam.AddAlias("e");
		cli_extparam.AddArgument(cli_cpu);
		cli_extparam.AddArgument(cli_target);
		cli_extparam.AddArgument(cli_ecu_id);
		SetHandler(cli_extparam, CliCommand.extparam);
		cli_root.AddCommand(cli_extparam);

		var cli_makeall = new Command(
			name: "makeall",
			description: "Convert all - tables, standadrd parameters, extended parameters."
		);

		cli_makeall.AddAlias("m");
		cli_makeall.AddArgument(cli_target);
		cli_makeall.AddArgument(cli_cal_id);
		cli_makeall.AddArgument(cli_ssmBaseString);
		SetHandler(cli_makeall, CliCommand.makeall);
		cli_root.AddCommand(cli_makeall);

		var cli_ecuf = new Command(
			name: "ecuf",
			description: "Convert EcuFlash xml definitions file."
		);

		cli_ecuf.AddAlias("f");
		cli_ecuf.AddArgument(cli_filename_xml);
		SetHandler(cli_ecuf, CliCommand.ecuf);
		cli_root.AddCommand(cli_ecuf);

		var cli_scoobyrom = new Command(
			name: "scoobyrom",
			description: "Convert ScoobyRom xml definitions file."
		);

		cli_scoobyrom.AddAlias("c");
		cli_scoobyrom.AddArgument(cli_filename_xml);
		SetHandler(cli_scoobyrom, CliCommand.scoobyrom);
		cli_root.AddCommand(cli_scoobyrom);

		void SetHandler(Command command, CliCommand c)
		{
			command.SetHandler
			<string,string,string,string,string,string,string,string,string,string,string,bool>
							(  (cli_cal_id,
								cli_cpu,
								cli_target,
								cli_ssmBaseString,
								cli_filename_xml,
								cli_ecu_id,
								cli_all_defs_dir,
								cli_ecu_defs,
								cli_logger_xml,
								cli_logger_dir,
								cli_logger_dtd,
								cli_keep_cal_id_symbol_case) =>
				{
					cmd = c;
					SetVariablesFromCli(cli_cal_id: cli_cal_id,
									cli_cpu: cli_cpu,
									cli_target: cli_target,
									cli_ssmBaseString: cli_ssmBaseString,
									cli_filename_xml: cli_filename_xml,
									cli_ecu_id: cli_ecu_id,
									cli_all_defs_dir: cli_all_defs_dir,
									cli_ecu_defs: cli_ecu_defs,
									cli_logger_xml: cli_logger_xml,
									cli_logger_dir: cli_logger_dir,
									cli_logger_dtd: cli_logger_dtd,
									cli_keep_cal_id_symbol_case: cli_keep_cal_id_symbol_case);
				},
				cli_cal_id,
				cli_cpu,
				cli_target,
				cli_ssmBaseString,
				cli_filename_xml,
				cli_ecu_id,
				cli_all_defs_dir,
				cli_ecu_defs,
				cli_logger_xml,
				cli_logger_dir,
				cli_logger_dtd,
				cli_keep_cal_id_symbol_case);
		}

		//Parse command line
		cli_root.Invoke(args);

		try
		{
			//Print header only in case of real work
			if (cmd > CliCommand.help)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("///////////////////////////////////////////////////////////////////////////////");
				stringBuilder.AppendLine($"// This file gernerated by DefToIdc version: {Assembly.GetExecutingAssembly().GetName().Version}");
				stringBuilder.AppendLine($"// running on mscorlib.dll version: {typeof(string).Assembly.GetName().Version}");
				stringBuilder.AppendLine("///////////////////////////////////////////////////////////////////////////////");
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("#include <idc.idc>");
				stringBuilder.AppendLine("static MakeNameExSafe(address, name, flag)");
				stringBuilder.AppendLine("{");
				stringBuilder.AppendLine("	if isTail(GetFlags(address))");
				stringBuilder.AppendLine("	{");
				stringBuilder.AppendLine("		MakeUnknown(address, 4, DOUNK_SIMPLE);");
				stringBuilder.AppendLine("	}");
				stringBuilder.AppendLine("	MakeNameEx(address, name, flag);");
				stringBuilder.AppendLine("}");
				stringBuilder.AppendLine();
				Console.Write(stringBuilder.ToString());
			}

			if (cmd == CliCommand.help)
			{
				cli_root.Invoke(["-h"]);
				return;
			}
			else if (cmd == CliCommand.tables)
			{
				string functionName = $"Tables_{calId}";
				WriteHeader1(functionName, $"Table definitions for {calId}");
				DefineTables(functionName, calId);
			}
			else if (cmd == CliCommand.stdparam)
			{
				string functionName = $"StdParams_{calId}";
				if (TryConvertBaseString(ssmBaseString, out UInt32? n))
				{
					UInt32 ssmBase = n.Value;
					WriteHeader1(functionName, string.Format("Standard parameter definitions for {0} bit {1}: {2} with SSM read vector base {3}", cpu, target, calId, ssmBase.ToString("X")));
					DefineStandardParameters(functionName, target, calId, ssmBase, cpu);
				}
				else
				{
					throw new FatalException($"{ssmBaseString} is not valid SSM base address");
				}
			}
			else if (cmd == CliCommand.extparam)
			{
					string functionName = $"ExtParams_{ecuId}";
					WriteHeader1(functionName, $"Extended parameter definitions for {cpu} bit {target}: {ecuId}");
					DefineExtendedParameters(functionName, target, ecuId, cpu);
			}
			else if (cmd == CliCommand.makeall)
			{
				string text5 = "Tables";
				string text6 = "StdParams";
				string text7 = "ExtParams";
				WriteHeader3(text5, text6, text7, $"All definitions for {target}: {calId} with SSM read vector base {ssmBaseString}");
				string[] array = DefineTables(text5, calId);

				if (String.IsNullOrEmpty(array[0]))
				{
					throw new FatalException($"Cannot find 'ecuid' tag for {calId} in {ecu_defs}");
				}
				if (TryConvertBaseString(ssmBaseString, out UInt32? n))
				{
					UInt32 ssmBase = n.Value;
					DefineStandardParameters(text6, target, calId, ssmBase, array[1]);
					DefineExtendedParameters(text7, target, array[0], array[1]);
				}
				else
				{
					throw new FatalException($"{ssmBaseString} is not valid SSM base address");
				}
			}
			else if (cmd == CliCommand.ecuf)
			{
				DefineEcufTables(filename_xml);
			}
			else if (cmd == CliCommand.scoobyrom)
			{
				DefineScoobyRom(filename_xml);
			}

		}
		catch(FatalException e)
		{
			Console.Error.WriteLine($"Error: {e.Message}. Terminating.");
		}
	}

	private static string[] DefineTables(string functionName, string calId)
	{
		if (!File.Exists(ecu_defs))
		{
			throw new FatalException($"Definitions file {ecu_defs} not found");
		}
		WriteHeader2(functionName);
		WriteTableNamesHeader();
		string[] array = WriteTableNames(calId);
		WriteFooter(functionName);
		return array;
	}

	private static void DefineStandardParameters(string functionName, string target, string calId, uint ssmBase, string cpu)
	{
		if (!File.Exists(logger_xml))
		{
			throw new FatalException($"{logger_xml} not found");
		}
		if (!File.Exists(logger_dtd))
		{
			throw new FatalException($"{logger_dtd} not found");
		}
		WriteHeader2(functionName);
		WriteStandardParameters(target, calId, ssmBase, cpu);
		WriteFooter(functionName);
	}

	private static void DefineExtendedParameters(string functionName, string target, string ecuId, string cpu)
	{
		if (!File.Exists(logger_xml))
		{
			throw new FatalException($"{logger_xml} not found");
		}
		if (!File.Exists(logger_dtd))
		{
			throw new FatalException($"{logger_dtd} not found");
		}
		WriteHeader2(functionName);
		WriteExtendedParameters(target, ecuId, cpu);
		WriteFooter(functionName);
	}

	private static void DefineEcufTables(string fileName)
	{
		if (!File.Exists(fileName))
		{
			throw new FatalException($"{fileName} not found");
		}
		WriteEcufTableNames(fileName);
	}

	private static void DefineScoobyRom(string fileName)
	{
		if (!File.Exists(fileName))
		{
			throw new FatalException($"{fileName} not found");
		}
		string functionName="Tables";
		WriteHeader1(functionName, "Table definitions");
		WriteHeader2(functionName);
		WriteTableNamesHeader();
		WriteScoobyRom(fileName);
		WriteFooter(functionName);
	}

	private static void WriteTableNamesHeader()
	{
		Console.WriteLine("auto referenceAddress;");
	}

	private static string[] WriteTableNames(string xmlId)
	{
		string[] result = PopulateTableNames(xmlId);
		WriteIdcTableNames();
		return result;
	}
	//Can do recursive calls
	private static string[] PopulateTableNames(string xmlId)
	{
		string ecuId = null;
		string memModel = null;
		int num = 0;
		string name = null;
		bool flag = false;
		int num2 = 0;
		string text3 = "32";
		string romBase = GetRomBase(xmlId);

		if (romBase == null)
		{
			throw new FatalException($"{xmlId} not found - either you specified wrong CAL ID or wrong definitions file");
		}
		if (romBase == "")
		{
			Console.Error.WriteLine($"Warning: {xmlId} 'rom' element does not have 'base' attribute set. Are your definitions alright? I'll continue anyway.");
			//"BASE" will be skipped anyway
			romBase="BASE";
		}
		string[] array = new string[2] { romBase, xmlId };
		using (Stream stream = File.OpenRead(ecu_defs))
		{
			XPathDocument xPathDocument = new XPathDocument(stream);
			XPathNavigator xPathNavigator = xPathDocument.CreateNavigator();
			string[] array2 = array;
			foreach (string loop_xmlId in array2)
			{
				num = 0;
				names.Clear();
				if (loop_xmlId.Contains("BASE"))
				{
					continue;
				}
				if (loop_xmlId.Equals(romBase))
				{
					Console.WriteLine($"print(\"Note: Marking tables using addresses from inherited base ROM: {loop_xmlId}\");");
					//Recursive call
					//Defs can inherit several other defs
					PopulateTableNames(loop_xmlId);
					continue;
				}
				string xpath = "/roms/rom/romid[xmlid='" + loop_xmlId + "']";
				XPathNodeIterator xPathNodeIterator = xPathNavigator.Select(xpath);
				xPathNodeIterator.MoveNext();
				xPathNavigator = xPathNodeIterator.Current;
				xPathNavigator.MoveToChild(XPathNodeType.Element);
				while (xPathNavigator.MoveToNext())
				{
					if (xPathNavigator.Name == "ecuid")
					{
						ecuId = xPathNavigator.InnerXml;
					}
					if (xPathNavigator.Name == "memmodel")
					{
						memModel = xPathNavigator.InnerXml;
						break;
					}
				}
				if (string.IsNullOrEmpty(ecuId))
				{
					throw new FatalException($"Could not find definition for {loop_xmlId}");
				}
				if (memModel.Contains("68HC"))
				{
					flag = true;
					text3 = "16";
				}
				int offset = GetRomLoadAddress(memModel);
				xPathNavigator.MoveToParent();
				while (xPathNavigator.MoveToNext())
				{
					if (!(xPathNavigator.Name == "table"))
					{
						continue;
					}
					string table_name = xPathNavigator.GetAttribute("name", "");
					string table_storageaddress = xPathNavigator.GetAttribute("storageaddress", "");
					if (!table_storageaddress.StartsWith("0x"))
					{
						table_storageaddress = "0x" + table_storageaddress;
					}
					table_name = ConvertName(table_name);
					UpdateTableList(table_name, table_storageaddress);
					if (flag)
					{
						name = ConvertName("Table_" + table_name);
						num2 = Convert.ToInt32(table_storageaddress, 16);
						num2--;
					}
					List<string> list = new List<string>();
					if (xPathNavigator.HasChildren)
					{
						xPathNavigator.MoveToChild(XPathNodeType.Element);
						do
						{
							string axis_type = xPathNavigator.GetAttribute("type", "");
							list.Add(axis_type);
							string axis_storageaddress = xPathNavigator.GetAttribute("storageaddress", "");
							if (!axis_storageaddress.StartsWith("0x"))
							{
								axis_storageaddress = "0x" + axis_storageaddress;
							}
							axis_type = ConvertName(table_name + "_" + axis_type);
							UpdateTableList(axis_type, axis_storageaddress);
						}
						while (xPathNavigator.MoveToNext());
						if (list.Count == 2 && list[0].ToUpper() == "X AXIS" && list[1].ToUpper() == "Y AXIS" && !flag)
						{
							string name2 = ConvertName("Table_" + table_name);
							UpdateTableList(name2, "2axis");
						}
						else if (list.Count == 1 && list[0].ToUpper() == "Y AXIS" && !flag)
						{
							string name2 = ConvertName("Table_" + table_name);
							UpdateTableList(name2, "1axis");
						}
						else if (list.Count > 0 && flag && list[0].ToUpper().Contains("AXIS"))
						{
							string address = "0x" + num2.ToString("X");
							UpdateTableList(name, address);
						}
						xPathNavigator.MoveToParent();
					}
					num++;
				}
				if (num < 1)
				{
					Console.WriteLine("// No tables found specifically for ROM " + loop_xmlId + ", used inherited ROM");
				}
			}
		}
		return new string[2] { ecuId, text3 };
	}

	/// <summary>
	/// Get base for id
	/// </summary>
	/// <param name="xmlId"></param>
	/// <returns>
	/// String id if found,
	/// empty string if base not found,
	/// null if xmlId not found.
	/// </returns>
	private static string GetRomBase(string xmlId)
	{
		string result = null;
		using (Stream stream = File.OpenRead(ecu_defs))
		{
			XPathDocument xPathDocument = new XPathDocument(stream);
			XPathNavigator xPathNavigator = xPathDocument.CreateNavigator();
			string xpath = $"/roms/rom/romid[xmlid='{xmlId}']";
			XPathNodeIterator xPathNodeIterator = xPathNavigator.Select(xpath);
			if (xPathNodeIterator.Count > 0)
			{
				xPathNodeIterator.MoveNext();
				xPathNavigator = xPathNodeIterator.Current;
				xPathNavigator.MoveToChild(XPathNodeType.Element);
				do
				{
					xPathNavigator.MoveToParent();
				}
				while (xPathNavigator.Name != "rom");

				result = xPathNavigator.GetAttribute("base", "");
			}
		}
		return result;
	}

	private static void WriteIdcTableNames()
	{
		foreach (KeyValuePair<string, string> table in tableList)
		{
			string key = table.Key;
			string value = table.Value;
			string text = "Table_" + key;
			string value2 = "";
			if (tableList.TryGetValue(text, out value2))
			{
				if (!key.StartsWith("Table_") && (value2.Equals("1axis") || value2.Equals("2axis")))
				{
					string text2 = "";
					if (value2.Equals("1axis"))
					{
						text2 = "8";
					}
					if (value2.Equals("2axis"))
					{
						text2 = "12";
					}
					MakeName(value, key);
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.AppendLine("referenceAddress = DfirstB(" + value + ");");
					stringBuilder.AppendLine("if (referenceAddress > 0)");
					stringBuilder.AppendLine("{");
					stringBuilder.AppendLine("    referenceAddress = referenceAddress - " + text2 + ";");
					string value3 = "    MakeNameExSafe(referenceAddress, \"" + text + "\", SN_CHECK);";
					stringBuilder.AppendLine(value3);
					stringBuilder.AppendLine("}");
					stringBuilder.AppendLine("else");
					stringBuilder.AppendLine("{");
					stringBuilder.AppendLine("    Message(\"No reference to " + key + "\\n\");");
					stringBuilder.AppendLine("}");
					Console.WriteLine(stringBuilder.ToString());
				}
				else
				{
					MakeName(value, key);
				}
			}
			else if (!key.StartsWith("Table_") || (key.StartsWith("Table_") && !value.Contains("axis")))
			{
				MakeName(value, key);
			}
		}
	}

	private static void WriteStandardParameters(string target, string ecuid, uint ssmBase, string cpu)
	{
		Console.WriteLine("auto addr;");
		if ((target == "ecu") | (target == "ECU"))
		{
			target = "2";
		}
		if ((target == "tcu") | (target == "TCU"))
		{
			target = "1";
		}
		string text = "PtrSsmGet_";
		string text2 = "SsmGet_";
		if (cpu.Equals("16"))
		{
			text = "PtrSsm_";
			text2 = "Ssm_";
			Console.WriteLine("auto opAddr, seg;");
			Console.WriteLine("if (SegByName(\"DATA\") != BADADDR) seg = 1;");
			Console.WriteLine("");
		}
		using (Stream stream = File.OpenRead($"{logger_xml}"))
		{
			XPathDocument xPathDocument = new XPathDocument(stream);
			XPathNavigator xPathNavigator = xPathDocument.CreateNavigator();
			string xpath = "/logger/protocols/protocol[@id='SSM']/parameters/parameter";
			XPathNodeIterator xPathNodeIterator = xPathNavigator.Select(xpath);
			string text3 = "";
			while (xPathNodeIterator.MoveNext())
			{
				XPathNavigator current = xPathNodeIterator.Current;
				if (current.GetAttribute("target", "") == target)
				{
					continue;
				}
				string attribute = current.GetAttribute("name", "");
				text3 = current.GetAttribute("id", "");
				if (cpu.Equals("16") && text3.Equals("P89"))
				{
					continue;
				}
				attribute = attribute + "_" + text3.Trim();
				string name = ConvertName(text + attribute);
				string name2 = ConvertName(text2 + attribute);
				if (!current.MoveToChild("address", ""))
				{
					continue;
				}
				string innerXml = xPathNodeIterator.Current.InnerXml;
				innerXml = innerXml.Substring(2);
				uint num = uint.Parse(innerXml, NumberStyles.HexNumber);
				if (!cpu.Equals("16") || num <= 399)
				{
					num *= 4;
					innerXml = "0x" + (num + ssmBase).ToString("X8");
					Console.WriteLine("MakeUnknown(" + innerXml + ", 4, DOUNK_SIMPLE);");
					Console.WriteLine("MakeDword(" + innerXml + ");");
					MakeName(innerXml, name);
					string value = "addr = Dword(" + innerXml + ");";
					Console.WriteLine(value);
					MakeName("addr", name2);
					if (cpu.Equals("16"))
					{
						string text4 = current.GetAttribute("length", "");
						FormatData("addr", text4);
						string arg = ConvertName("SsmGet_" + attribute);
						StringBuilder stringBuilder = new StringBuilder();
						stringBuilder.AppendLine("opAddr = FindImmediate(0, 0x21, (addr - 0x20000));");
						stringBuilder.AppendLine("addr = GetFunctionAttr(opAddr, FUNCATTR_START);");
						stringBuilder.AppendLine("if (addr != BADADDR)");
						stringBuilder.AppendLine("{");
						stringBuilder.AppendLine("    if (seg)");
						stringBuilder.AppendLine("    {");
						stringBuilder.AppendLine("        OpAlt(opAddr, 0, \"\");");
						stringBuilder.AppendLine("        OpOff(MK_FP(\"ROM\", opAddr), 0, 0x20000);");
						stringBuilder.AppendLine("    }");
						stringBuilder.AppendLine("    add_dref(opAddr, Dword(" + innerXml + "), dr_I);");
						string value2 = "    MakeNameExSafe(addr, \"" + arg + "\", SN_CHECK);";
						stringBuilder.AppendLine(value2);
						stringBuilder.AppendLine("}");
						stringBuilder.AppendLine("else");
						stringBuilder.AppendLine("{");
						stringBuilder.AppendLine("    Message(\"No reference to " + attribute + "\\n\");");
						stringBuilder.AppendLine("}");
						Console.Write(stringBuilder.ToString());
					}
					Console.WriteLine();
				}
			}
		
			IDictionary<string, Array> dictionary = new Dictionary<string, Array>();
			xpath = "/logger/protocols/protocol[@id='SSM']/switches/switch";
			xPathNodeIterator = xPathNavigator.Select(xpath);
			string text5 = "";
			while (xPathNodeIterator.MoveNext())
			{
				XPathNavigator current = xPathNodeIterator.Current;
				if (!(current.GetAttribute("target", "") == target))
				{
					text3 = current.GetAttribute("id", "");
					text3 = text3.Replace("S", "");
					string attribute2 = current.GetAttribute("byte", "");
					attribute2 = attribute2.Substring(2);
					text5 = current.GetAttribute("bit", "");
					Array value3;
					if (!dictionary.TryGetValue(attribute2, out value3))
					{
						string[] value4 = new string[8] { "x", "x", "x", "x", "x", "x", "x", "x" };
						dictionary.Add(attribute2, value4);
					}
					if (dictionary.TryGetValue(attribute2, out value3))
					{
						uint num2 = uint.Parse(text5, NumberStyles.HexNumber);
						value3.SetValue(text3.Trim(), num2);
						Array.Copy(value3, dictionary[attribute2], value3.Length);
					}
				}
			}
			PrintSwitches(dictionary, ssmBase, cpu);
		}
	}

	private static void WriteExtendedParameters(string target, string ecuid, string cpu)
	{
		if ((target == "ecu") | (target == "ECU"))
		{
			target = "2";
		}
		if ((target == "tcu") | (target == "TCU"))
		{
			target = "1";
		}
		using (Stream stream = File.OpenRead($"{logger_xml}"))
		{
			XPathDocument xPathDocument = new XPathDocument(stream);
			XPathNavigator xPathNavigator = xPathDocument.CreateNavigator();
			string xpath = "/logger/protocols/protocol[@id='SSM']/ecuparams/ecuparam/ecu[contains(@id, '" + ecuid + "')]/address";
			XPathNodeIterator xPathNodeIterator = xPathNavigator.Select(xpath);
			while (xPathNodeIterator.MoveNext())
			{
				string innerXml = xPathNodeIterator.Current.InnerXml;
				innerXml = innerXml.Substring(2);
				uint num = uint.Parse(innerXml, NumberStyles.HexNumber);
				if (cpu.Contains("32"))
				{
					num |= 0xFF000000u;
				}
				innerXml = "0x" + num.ToString("X8");
				XPathNavigator current = xPathNodeIterator.Current;
				string attribute = current.GetAttribute("length", "");
				current.MoveToParent();
				current.MoveToParent();
				if (!(current.GetAttribute("target", "") == target))
				{
					string attribute2 = current.GetAttribute("name", "");
					string attribute3 = current.GetAttribute("id", "");
					attribute2 = "E_" + ConvertName(attribute2) + "_" + attribute3.Trim();
					MakeName(innerXml, attribute2);
					FormatData(innerXml, attribute);
				}
			}
		}
	}

	private static string[] WriteEcufTableNames(string filename)
	{
		string text = null;
		string value = null;
		string memModel = null;
		int num = 0;
		string name = null;
		bool romIs68HC = false;
		int num2 = 0;
		using (Stream stream = File.OpenRead(filename))
		{
			XPathDocument xPathDocument = new XPathDocument(stream);
			XPathNavigator xPathNavigator = xPathDocument.CreateNavigator();
			int offset = GetRomLoadAddress(GetMemModel_EF_SR(xPathNavigator));
			num = 0;
			names.Clear();
			string xpath = "/rom/romid/xmlid";
			XPathNodeIterator xPathNodeIterator = xPathNavigator.Select(xpath);
			xPathNodeIterator.MoveNext();
			xPathNavigator = xPathNodeIterator.Current;
			xPathNavigator.MoveToChild(XPathNodeType.Element);
			text = xPathNavigator.InnerXml;
			string functionName = "Tables_" + text;
			WriteHeader1(functionName, "Table definitions for " + text + "");
			WriteHeader2(functionName);
			Console.WriteLine("auto referenceAddress;");
			while (xPathNavigator.MoveToNext())
			{
				if (xPathNavigator.Name == "ecuid")
				{
					value = xPathNavigator.InnerXml;
				}
				if (xPathNavigator.Name == "memmodel")
				{
					memModel = xPathNavigator.InnerXml;
					break;
				}
			}
			if (string.IsNullOrEmpty(value))
			{
				throw new FatalException($"Could not find ECU ID for {text}");
			}
			if (memModel.Contains("68HC"))
			{
				romIs68HC = true;
			}
			xPathNavigator.MoveToParent();
			while (xPathNavigator.MoveToNext())
			{
				if (!(xPathNavigator.Name == "table"))
				{
					continue;
				}
				string table_name = xPathNavigator.GetAttribute("name", "");
				string table_address = xPathNavigator.GetAttribute("address", "");
				//Not sure if EcuFlash has support for <offset>
				table_address = Add(table_address, offset);
				if (!table_address.StartsWith("0x"))
				{
					table_address = "0x" + table_address.ToUpper();
				}
				table_name = ConvertName(table_name);
				UpdateTableList(table_name, table_address);
				if (romIs68HC)
				{
					name = ConvertName("Table_" + table_name);
					num2 = Convert.ToInt32(table_address, 16);
					num2--;
				}
				List<string> list = new List<string>();
				if (xPathNavigator.HasChildren)
				{
					xPathNavigator.MoveToChild(XPathNodeType.Element);
					do
					{
						string axis_name = xPathNavigator.GetAttribute("name", "");
						if (!axis_name.ToUpper().Contains("AXIS"))
						{
							axis_name += "_Axis";
						}
						list.Add(axis_name);
						string axis_address = xPathNavigator.GetAttribute("address", "");
						//Not sure if EcuFlash has support for <offset>
						axis_address = Add(axis_address, offset);
						if (!axis_address.StartsWith("0x"))
						{
							axis_address = "0x" + axis_address.ToUpper();
						}
						if (axis_address != "0x")
						{
							axis_name = ConvertName(table_name + "_" + axis_name);
							UpdateTableList(axis_name, axis_address);
						}
					}
					while (xPathNavigator.MoveToNext());
					if (list.Count == 2 && list[0].ToUpper() == "X_AXIS" && list[1].ToUpper() == "Y_AXIS" && !romIs68HC)
					{
						string name2 = ConvertName("Table_" + table_name);
						UpdateTableList(name2, "2axis");
					}
					else if (list.Count == 1 && list[0].ToUpper() == "Y_AXIS" && !romIs68HC)
					{
						string name2 = ConvertName("Table_" + table_name);
						UpdateTableList(name2, "1axis");
					}
					else if (list.Count > 0 && romIs68HC && list[0].ToUpper().Contains("AXIS"))
					{
						string axis_address = "0x" + num2.ToString("X");
						UpdateTableList(name, axis_address);
					}
					xPathNavigator.MoveToParent();
				}
				num++;
			}
			if (num < 1)
			{
				Console.WriteLine("// No tables found for ROM " + text + ", used inherited ROM");
			}
			WriteIdcTableNames();
			WriteFooter(functionName);
		}
		return null;
	}

	private static void WriteScoobyRom(string fileName)
	{
		PopulateScoobyRom(fileName);
		WriteIdcTableNames();
	}

	/// <summary>
	/// Get memmodel string for EcuFlash and ScoobyRom defs
	/// </summary>
	/// <param name="xPathNavigator"></param>
	/// <returns></returns>
	private static string GetMemModel_EF_SR (XPathNavigator xPathNavigator)
	{
		string memModel = "";
		string xpath = "/rom/romid/memmodel";
		XPathNodeIterator xPathNodeIterator = xPathNavigator.Select(xpath);
		while (xPathNodeIterator.MoveNext())
		{
			XPathNavigator xpn = xPathNodeIterator.Current;
			xpn.MoveToChild(XPathNodeType.Element);
			do
			{
				if (xpn.Name == "memmodel")
				{
					memModel = xpn.InnerXml;
					goto exit;
				}
			} while (xpn.MoveToNext());
		}
		exit:
		return string.IsNullOrEmpty(memModel) ? "" : memModel;
	}

	private static int GetRomLoadAddress(string memModel)
	{
		int addr = 0;
		if (memModel != null)
		{
			if (memModel.StartsWith("MPC5746"))
			{
				addr = 0x8F9C000;
			}
		}
		return addr;
	}

	/// <summary>
	/// Convert hex string to number, add offset and convert back to string
	/// </summary>
	/// <param name="hex_address">Hex string</param>
	/// <param name="offset">Number</param>
	/// <returns></returns>
	private static string Add(string hex_address, int offset)
	{
		int addr = Convert.ToInt32(hex_address, 16);
		addr += offset;
		return addr.ToString("X");
	}

	private static void PopulateScoobyRom(string fileName)
	{
		using (Stream stream = File.OpenRead(fileName))
		{
			XPathDocument xPathDocument = new XPathDocument(stream);
			XPathNavigator xPathNavigator = xPathDocument.CreateNavigator();
			names.Clear();

			string xpath = "/rom";
			XPathNodeIterator xPathNodeIterator = xPathNavigator.Select(xpath);
			xPathNodeIterator.MoveNext();
			xPathNavigator = xPathNodeIterator.Current;
			xPathNavigator.MoveToChild(XPathNodeType.Element);

			while (xPathNavigator.MoveToNext())
			{
				string element_name = xPathNavigator.Name;
				if (element_name != "table2D" && element_name != "table3D")
				{
					continue;
				}
				string table_name = xPathNavigator.GetAttribute("name", "");
				string table_storageaddress = xPathNavigator.GetAttribute("storageaddress", "");
				if (!table_storageaddress.StartsWith("0x"))
				{
					table_storageaddress = "0x" + table_storageaddress;
				}
				string converted_table_name;
				if (String.IsNullOrEmpty(table_name))
				{
					table_name = $"{element_name} at {table_storageaddress}";
				}
				converted_table_name = ConvertName(table_name);
				UpdateTableList(converted_table_name, table_storageaddress);

				if (xPathNavigator.HasChildren)
				{
					xPathNavigator.MoveToChild(XPathNodeType.Element);
					do
					{
						string inner_element_name = xPathNavigator.Name;
						if (inner_element_name.StartsWith("axis") || inner_element_name == "values")
						{
							string inner_element_addr = xPathNavigator.GetAttribute("storageaddress", "");
							if (!inner_element_addr.StartsWith("0x"))
							{
								inner_element_addr = "0x" + inner_element_addr;
							}
							inner_element_name = ConvertName($"{table_name} {inner_element_name}");
							UpdateTableList(inner_element_name, inner_element_addr);
						}
					} while (xPathNavigator.MoveToNext());

					xPathNavigator.MoveToParent();
				}
			}
		}
	}
	private static void WriteHeader1(string functionName, string description)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("///////////////////////////////////////////////////////////////////////////////");
		stringBuilder.AppendLine("// " + description);
		stringBuilder.AppendLine("///////////////////////////////////////////////////////////////////////////////");
		stringBuilder.AppendLine("static main ()");
		stringBuilder.AppendLine("{");
		stringBuilder.AppendLine("    " + functionName + " ();");
		stringBuilder.AppendLine("}");
		Console.WriteLine(stringBuilder.ToString());
	}

	private static void WriteHeader2(string functionName)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("static " + functionName + " ()");
		stringBuilder.AppendLine("{");
		stringBuilder.AppendLine("Message(\"--- Now marking " + functionName + " ---\\n\");");
		Console.Write(stringBuilder.ToString());
	}

	private static void WriteHeader3(string functionName1, string functionName2, string functionName3, string description)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("///////////////////////////////////////////////////////////////////////////////");
		stringBuilder.AppendLine("// " + description);
		stringBuilder.AppendLine("///////////////////////////////////////////////////////////////////////////////");
		stringBuilder.AppendLine("static main ()");
		stringBuilder.AppendLine("{");
		stringBuilder.AppendLine("    " + functionName1 + " ();");
		stringBuilder.AppendLine("    " + functionName2 + " ();");
		stringBuilder.AppendLine("    " + functionName3 + " ();");
		stringBuilder.AppendLine("}");
		Console.WriteLine(stringBuilder.ToString());
	}

	private static void WriteFooter(string functionName)
	{
		Console.WriteLine("}   // end of " + functionName);
		Console.WriteLine();
	}

	private static void PrintSwitches(IDictionary<string, Array> switchList, uint ssmBase, string cpu)
	{
		Console.WriteLine("// Switch Bit Position Name format: Switches_b7_b6_b5_b4_b3_b2_b1_b0");
		string text = "PtrSsmGet_";
		string text2 = "SsmGet_";
		if (cpu.Equals("16"))
		{
			text = "PtrSsm_";
			text2 = "Ssm_";
		}
		foreach (KeyValuePair<string, Array> @switch in switchList)
		{
			string[] array = new string[8];
			Array.Copy(@switch.Value, array, 8);
			string text3 = "";
			for (int num = array.Length; num != 0; num--)
			{
				text3 = "" + text3 + "_"+array[num - 1];
			}
			string text4 = "Switches" + text3;
			string name = ConvertName(text + text4);
			string name2 = ConvertName(text2 + text4);
			uint num2 = uint.Parse(@switch.Key, NumberStyles.HexNumber);
			if (!cpu.Equals("16") || num2 <= 399)
			{
				num2 *= 4;
				string text5 = "0x" + (num2 + ssmBase).ToString("X8");
				Console.WriteLine("MakeUnknown(" + text5 + ", 4, DOUNK_SIMPLE);");
				Console.WriteLine("MakeDword(" + text5 + ");");
				MakeName(text5, name);
				string value = "addr = Dword(" + text5 + ");";
				Console.WriteLine(value);
				if (cpu.Equals("16"))
				{
					FormatData("addr", "1");
				}
				MakeName("addr", name2);
				Console.WriteLine();
			}
		}
	}

	private static void MakeName(string address, string name)
	{
		if (address.Length > 0 && name.Length > 0)
		{
			string value;
			value = $"MakeNameExSafe({address}, \"{name}\", SN_CHECK);";
			Console.WriteLine(value);
		}
	}

	private static void UpdateTableList(string name, string address)
	{
		if (HexTryParse(address, out int? _) && address.Length > 0 && !String.IsNullOrEmpty(name))
		{
			if (tableList.TryGetValue(name, out _))
			{
				tableList[name] = address;
			}
			else
			{
				tableList.Add(name, address);
			}
		}
	}

	private static string ConvertName(string original)
	{
		original = original.Replace(")(", "_");
		StringBuilder stringBuilder = new StringBuilder(original.Length);
		string text = original;
		foreach (char c in text)
		{
			if (char.IsLetterOrDigit(c))
			{
				stringBuilder.Append(c);
			}
			else if (c == '_')
			{
				stringBuilder.Append(c);
			}
			else if (char.IsWhiteSpace(c))
			{
				stringBuilder.Append('_');
			}
			else if (c == '*')
			{
				stringBuilder.Append("Ext");
			}
		}
		string text2 = stringBuilder.ToString();
		while (names.Contains(text2))
		{
			text2 += "_";
		}
		names.Add(text2);
		return text2;
	}

	private static void FormatData(string address, string length)
	{
		string text = "";
		if (length == "" || length == "1")
		{
			text = "MakeByte";
			length = "1";
		}
		else if (length == "2")
		{
			text = "MakeWord";
		}
		else if (length == "4")
		{
			text = "MakeFloat";
		}
		if (text != "")
		{
			string value = "MakeUnknown(" + address + ", " + length + ", DOUNK_SIMPLE);";
			Console.WriteLine(value);
			string value2 = "" + text + "(" + address + ");";
			Console.WriteLine(value2);
		}
	}

	private static bool TryConvertBaseString(string ssmBaseString, out UInt32? ssmBase)
	{
		ssmBase = null;
		bool result = false;

		if (HexTryParse(ssmBaseString, out ssmBase))
		{
			const UInt32 ssmBaseLowerLimit = 131072;//0x20000
			if (ssmBase < ssmBaseLowerLimit) 
			{
				UInt32? adjustedSsmBase = ssmBase + ssmBaseLowerLimit;
				Console.Error.WriteLine($"Warning: SSM base adjusted from 0x{ssmBase.Value.ToString("X")} to 0x{adjustedSsmBase.Value.ToString("X")}");
				ssmBase = adjustedSsmBase;
			}
			result = true;
		}

		return result;
	}

}
}