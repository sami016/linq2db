﻿//---------------------------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated by T4Model template for T4 (https://github.com/linq2db/linq2db).
//    Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
//---------------------------------------------------------------------------------------------------

#pragma warning disable 1591
#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

using Tests.T4.Model;

namespace Tests.T4.Model
{
	[CustomValidation(typeof(TestClass1.CustomValidator), "ValidateEditableString1")]
	[CustomValidation(typeof(TestClass1.CustomValidator), "ValidateEditableString2")]
	[CustomValidation(typeof(TestClass1.CustomValidator), "ValidateEditableLong1")]
	[CustomValidation(typeof(TestClass1.CustomValidator), "ValidateEditableInt1")]
	public partial class TestClass1 : IEditableObject, INotifyPropertyChanged, INotifyPropertyChanging
	{
		public TestClass1()
		{
			AcceptChanges();
		}

		#region EditableString1 : string

		private string  _currentEditableString1 = "12345";
		private string _originalEditableString1 = "12345";
		public  string          EditableString1
		{
			get { return _currentEditableString1; }
			set
			{
				if (_currentEditableString1 != value)
				{
					OnEditableString1Changing();

					BeforeEditableString1Changed(value);
					_currentEditableString1 = value;
					AfterEditableString1Changed();

					OnEditableString1Changed();
				}
			}
		}

		#region EditableObject support

		public void AcceptEditableString1Changes()
		{
			_originalEditableString1 = _currentEditableString1;
		}

		public void RejectEditableString1Changes()
		{
			EditableString1 = _originalEditableString1;
		}

		public bool IsEditableString1Dirty
		{
			get { return _currentEditableString1 != _originalEditableString1; }
		}

		#endregion

		#region INotifyPropertyChanged support

		partial void BeforeEditableString1Changed(string newValue);
		partial void AfterEditableString1Changed ();

		public const string NameOfEditableString1 = "EditableString1";

		private static readonly PropertyChangedEventArgs _editableString1ChangedEventArgs = new PropertyChangedEventArgs(NameOfEditableString1);

		private void OnEditableString1Changed()
		{
			OnPropertyChanged(_editableString1ChangedEventArgs);
		}

		#endregion

		#region INotifyPropertyChanging support

		private static readonly PropertyChangingEventArgs _editableString1ChangingEventArgs = new PropertyChangingEventArgs(NameOfEditableString1);

		private void OnEditableString1Changing()
		{
			OnPropertyChanging(_editableString1ChangingEventArgs);
		}

		#endregion

		#endregion

		#region EditableString2 : string?

		private string?  _currentEditableString2 = null;
		private string? _originalEditableString2 = null;
		public  string?          EditableString2
		{
			get { return _currentEditableString2; }
			set
			{
				if (_currentEditableString2 != value)
				{
					OnEditableString2Changing();

					BeforeEditableString2Changed(value);
					_currentEditableString2 = value;
					AfterEditableString2Changed();

					OnEditableString2Changed();
				}
			}
		}

		#region EditableObject support

		public void AcceptEditableString2Changes()
		{
			_originalEditableString2 = _currentEditableString2;
		}

		public void RejectEditableString2Changes()
		{
			EditableString2 = _originalEditableString2;
		}

		public bool IsEditableString2Dirty
		{
			get { return _currentEditableString2 != _originalEditableString2; }
		}

		#endregion

		#region INotifyPropertyChanged support

		partial void BeforeEditableString2Changed(string? newValue);
		partial void AfterEditableString2Changed ();

		public const string NameOfEditableString2 = "EditableString2";

		private static readonly PropertyChangedEventArgs _editableString2ChangedEventArgs = new PropertyChangedEventArgs(NameOfEditableString2);

		private void OnEditableString2Changed()
		{
			OnPropertyChanged(_editableString2ChangedEventArgs);
		}

		#endregion

		#region INotifyPropertyChanging support

		private static readonly PropertyChangingEventArgs _editableString2ChangingEventArgs = new PropertyChangingEventArgs(NameOfEditableString2);

		private void OnEditableString2Changing()
		{
			OnPropertyChanging(_editableString2ChangingEventArgs);
		}

		#endregion

		#endregion

		#region EditableLong1 : long

		private long  _currentEditableLong1 = 12345;
		private long _originalEditableLong1 = 12345;
		public  long          EditableLong1
		{
			get { return _currentEditableLong1; }
			set
			{
				if (_currentEditableLong1 != value)
				{
					OnEditableLong1Changing();

					BeforeEditableLong1Changed(value);
					_currentEditableLong1 = value;
					AfterEditableLong1Changed();

					OnEditableLong1Changed();
				}
			}
		}

		#region EditableObject support

		public void AcceptEditableLong1Changes()
		{
			_originalEditableLong1 = _currentEditableLong1;
		}

		public void RejectEditableLong1Changes()
		{
			EditableLong1 = _originalEditableLong1;
		}

		public bool IsEditableLong1Dirty
		{
			get { return _currentEditableLong1 != _originalEditableLong1; }
		}

		#endregion

		#region INotifyPropertyChanged support

		partial void BeforeEditableLong1Changed(long newValue);
		partial void AfterEditableLong1Changed ();

		public const string NameOfEditableLong1 = "EditableLong1";

		private static readonly PropertyChangedEventArgs _editableLong1ChangedEventArgs = new PropertyChangedEventArgs(NameOfEditableLong1);

		private void OnEditableLong1Changed()
		{
			OnPropertyChanged(_editableLong1ChangedEventArgs);
		}

		#endregion

		#region INotifyPropertyChanging support

		private static readonly PropertyChangingEventArgs _editableLong1ChangingEventArgs = new PropertyChangingEventArgs(NameOfEditableLong1);

		private void OnEditableLong1Changing()
		{
			OnPropertyChanging(_editableLong1ChangingEventArgs);
		}

		#endregion

		#endregion

		#region EditableInt1 : int

		private int  _currentEditableInt1;
		private int _originalEditableInt1;
		public  int          EditableInt1
		{
			get { return _currentEditableInt1; }
			set
			{
				if (_currentEditableInt1 != value)
				{
					OnEditableInt1Changing();

					BeforeEditableInt1Changed(value);
					_currentEditableInt1 = value;
					AfterEditableInt1Changed();

					OnEditableInt1Changed();
				}
			}
		}

		#region EditableObject support

		public void AcceptEditableInt1Changes()
		{
			_originalEditableInt1 = _currentEditableInt1;
		}

		public void RejectEditableInt1Changes()
		{
			EditableInt1 = _originalEditableInt1;
		}

		public bool IsEditableInt1Dirty
		{
			get { return _currentEditableInt1 != _originalEditableInt1; }
		}

		#endregion

		#region INotifyPropertyChanged support

		partial void BeforeEditableInt1Changed(int newValue);
		partial void AfterEditableInt1Changed ();

		public const string NameOfEditableInt1 = "EditableInt1";

		private static readonly PropertyChangedEventArgs _editableInt1ChangedEventArgs = new PropertyChangedEventArgs(NameOfEditableInt1);

		private void OnEditableInt1Changed()
		{
			OnPropertyChanged(_editableInt1ChangedEventArgs);
		}

		#endregion

		#region INotifyPropertyChanging support

		private static readonly PropertyChangingEventArgs _editableInt1ChangingEventArgs = new PropertyChangingEventArgs(NameOfEditableInt1);

		private void OnEditableInt1Changing()
		{
			OnPropertyChanging(_editableInt1ChangingEventArgs);
		}

		#endregion

		#endregion

		#region EditableInt3 : int

		private int  _currentEditableInt3;
		private int _originalEditableInt3;
		public  int          EditableInt3
		{
			get { return _currentEditableInt3; }
			set
			{
				if (_currentEditableInt3 != value)
				{
					OnEditableInt1Changing();
					OnEditableInt3Changing();

					BeforeEditableInt3Changed(value);
					_currentEditableInt3 = value;
					AfterEditableInt3Changed();

					OnEditableInt1Changed();
					OnEditableInt3Changed();
				}
			}
		}

		#region EditableObject support

		public void AcceptEditableInt3Changes()
		{
			_originalEditableInt3 = _currentEditableInt3;
		}

		public void RejectEditableInt3Changes()
		{
			EditableInt3 = _originalEditableInt3;
		}

		public bool IsEditableInt3Dirty
		{
			get { return _currentEditableInt3 != _originalEditableInt3; }
		}

		#endregion

		#region INotifyPropertyChanged support

		partial void BeforeEditableInt3Changed(int newValue);
		partial void AfterEditableInt3Changed ();

		public const string NameOfEditableInt3 = "EditableInt3";

		private static readonly PropertyChangedEventArgs _editableInt3ChangedEventArgs = new PropertyChangedEventArgs(NameOfEditableInt3);

		private void OnEditableInt3Changed()
		{
			OnPropertyChanged(_editableInt3ChangedEventArgs);
		}

		#endregion

		#region INotifyPropertyChanging support

		private static readonly PropertyChangingEventArgs _editableInt3ChangingEventArgs = new PropertyChangingEventArgs(NameOfEditableInt3);

		private void OnEditableInt3Changing()
		{
			OnPropertyChanging(_editableInt3ChangingEventArgs);
		}

		#endregion

		#endregion

		#region EditableDouble1 : double

		private double  _currentEditableDouble1;
		private double _originalEditableDouble1;
		public  double          EditableDouble1
		{
			get { return _currentEditableDouble1; }
			set
			{
				if (_currentEditableDouble1 != value)
				{
					OnEditableDouble1Changing();

					BeforeEditableDouble1Changed(value);
					_currentEditableDouble1 = value;
					AfterEditableDouble1Changed();

					OnEditableDouble1Changed();
				}
			}
		}

		#region EditableObject support

		public void AcceptEditableDouble1Changes()
		{
			_originalEditableDouble1 = _currentEditableDouble1;
		}

		public void RejectEditableDouble1Changes()
		{
			EditableDouble1 = _originalEditableDouble1;
		}

		public bool IsEditableDouble1Dirty
		{
			get { return Math.Abs(_currentEditableDouble1 - _originalEditableDouble1) <= 16 * Double.Epsilon * Math.Max(Math.Abs(_currentEditableDouble1), Math.Abs(_originalEditableDouble1)); }
		}

		#endregion

		#region INotifyPropertyChanged support

		partial void BeforeEditableDouble1Changed(double newValue);
		partial void AfterEditableDouble1Changed ();

		public const string NameOfEditableDouble1 = "EditableDouble1";

		private static readonly PropertyChangedEventArgs _editableDouble1ChangedEventArgs = new PropertyChangedEventArgs(NameOfEditableDouble1);

		private void OnEditableDouble1Changed()
		{
			OnPropertyChanged(_editableDouble1ChangedEventArgs);
		}

		#endregion

		#region INotifyPropertyChanging support

		private static readonly PropertyChangingEventArgs _editableDouble1ChangingEventArgs = new PropertyChangingEventArgs(NameOfEditableDouble1);

		private void OnEditableDouble1Changing()
		{
			OnPropertyChanging(_editableDouble1ChangingEventArgs);
		}

		#endregion

		#endregion

		#region NotifiedProp1 : string?

		private string? _notifiedProp1;
		public  string?  NotifiedProp1
		{
			get { return _notifiedProp1; }
			set
			{
				if (_notifiedProp1 != value)
				{
					OnNotifiedProp2Changing();
					OnNotifiedProp3Changing();

					BeforeNotifiedProp1Changed(value);
					_notifiedProp1 = value;
					AfterNotifiedProp1Changed();

					OnNotifiedProp2Changed();
					OnNotifiedProp3Changed();
				}
			}
		}

		#region INotifyPropertyChanged support

		partial void BeforeNotifiedProp1Changed(string? newValue);
		partial void AfterNotifiedProp1Changed ();

		public const string NameOfNotifiedProp1 = "NotifiedProp1";

		private static readonly PropertyChangedEventArgs _notifiedProp1ChangedEventArgs = new PropertyChangedEventArgs(NameOfNotifiedProp1);

		private void OnNotifiedProp1Changed()
		{
			OnPropertyChanged(_notifiedProp1ChangedEventArgs);
		}

		#endregion

		#region INotifyPropertyChanging support

		private static readonly PropertyChangingEventArgs _notifiedProp1ChangingEventArgs = new PropertyChangingEventArgs(NameOfNotifiedProp1);

		private void OnNotifiedProp1Changing()
		{
			OnPropertyChanging(_notifiedProp1ChangingEventArgs);
		}

		#endregion

		#endregion

		#region NotifiedProp2 : int

		private int _notifiedProp2 = 1;
		public  int  NotifiedProp2
		{
			get { return _notifiedProp2; }
			set
			{
				if (_notifiedProp2 != value)
				{
					OnNotifiedProp2Changing();

					BeforeNotifiedProp2Changed(value);
					_notifiedProp2 = value;
					AfterNotifiedProp2Changed();

					OnNotifiedProp2Changed();
				}
			}
		}

		#region INotifyPropertyChanged support

		partial void BeforeNotifiedProp2Changed(int newValue);
		partial void AfterNotifiedProp2Changed ();

		public const string NameOfNotifiedProp2 = "NotifiedProp2";

		private static readonly PropertyChangedEventArgs _notifiedProp2ChangedEventArgs = new PropertyChangedEventArgs(NameOfNotifiedProp2);

		private void OnNotifiedProp2Changed()
		{
			OnPropertyChanged(_notifiedProp2ChangedEventArgs);
		}

		#endregion

		#region INotifyPropertyChanging support

		private static readonly PropertyChangingEventArgs _notifiedProp2ChangingEventArgs = new PropertyChangingEventArgs(NameOfNotifiedProp2);

		private void OnNotifiedProp2Changing()
		{
			OnPropertyChanging(_notifiedProp2ChangingEventArgs);
		}

		#endregion

		#endregion

		#region NotifiedProp3 : long

		public long NotifiedProp3
		{
			get { return 1; }
		}

		#region INotifyPropertyChanged support

		public const string NameOfNotifiedProp3 = "NotifiedProp3";

		private static readonly PropertyChangedEventArgs _notifiedProp3ChangedEventArgs = new PropertyChangedEventArgs(NameOfNotifiedProp3);

		private void OnNotifiedProp3Changed()
		{
			OnPropertyChanged(_notifiedProp3ChangedEventArgs);
		}

		#endregion

		#region INotifyPropertyChanging support

		private static readonly PropertyChangingEventArgs _notifiedProp3ChangingEventArgs = new PropertyChangingEventArgs(NameOfNotifiedProp3);

		private void OnNotifiedProp3Changing()
		{
			OnPropertyChanging(_notifiedProp3ChangingEventArgs);
		}

		#endregion

		#endregion

		#region IDProp3 : string?

		private string? _idProp3;
		public  string?  IDProp3
		{
			get { return _idProp3; }
			set
			{
				if (_idProp3 != value)
				{
					OnIDProp3Changing();

					BeforeIDProp3Changed(value);
					_idProp3 = value;
					AfterIDProp3Changed();

					OnIDProp3Changed();
				}
			}
		}

		#region INotifyPropertyChanged support

		partial void BeforeIDProp3Changed(string? newValue);
		partial void AfterIDProp3Changed ();

		public const string NameOfIDProp3 = "IDProp3";

		private static readonly PropertyChangedEventArgs _idProp3ChangedEventArgs = new PropertyChangedEventArgs(NameOfIDProp3);

		private void OnIDProp3Changed()
		{
			OnPropertyChanged(_idProp3ChangedEventArgs);
		}

		#endregion

		#region INotifyPropertyChanging support

		private static readonly PropertyChangingEventArgs _idProp3ChangingEventArgs = new PropertyChangingEventArgs(NameOfIDProp3);

		private void OnIDProp3Changing()
		{
			OnPropertyChanging(_idProp3ChangingEventArgs);
		}

		#endregion

		#endregion

		#region IDProp4 : string

		private string _idProp4 = string.Empty;
		public  string  IDProp4
		{
			get { return _idProp4; }
			set
			{
				if (_idProp4 != value)
				{
					OnIDProp4Changing();

					BeforeIDProp4Changed(value);
					_idProp4 = value;
					AfterIDProp4Changed();

					OnIDProp4Changed();
				}
			}
		}

		#region INotifyPropertyChanged support

		partial void BeforeIDProp4Changed(string newValue);
		partial void AfterIDProp4Changed ();

		public const string NameOfIDProp4 = "IDProp4";

		private static readonly PropertyChangedEventArgs _idProp4ChangedEventArgs = new PropertyChangedEventArgs(NameOfIDProp4);

		private void OnIDProp4Changed()
		{
			OnPropertyChanged(_idProp4ChangedEventArgs);
		}

		#endregion

		#region INotifyPropertyChanging support

		private static readonly PropertyChangingEventArgs _idProp4ChangingEventArgs = new PropertyChangingEventArgs(NameOfIDProp4);

		private void OnIDProp4Changing()
		{
			OnPropertyChanging(_idProp4ChangingEventArgs);
		}

		#endregion

		#endregion

		#region Test Region

		/// <summary>
		/// 123
		/// </summary>
		[XmlArrayItem(typeof(int), DataType="List")                                                ] public int     Field1;
#if AAA
		[                                            XmlArray("Name1")                             ] public string? Field2;
#endif
		[                                            XmlArray("Name3")                             ] public string  Field22 = string.Empty;
		[XmlArrayItem(typeof(int), DataType="List"), XmlArray("Name21"), XmlArrayItem(typeof(char))] public string? Field21;
		[XmlAttribute("Name1", typeof(int)),         XmlArray("N2")                                ] public string? Field221  { get; set; }
		                                                                                             public string? Field2212;
		[XmlAttribute("Nm1", typeof(int))                                                          ] public string? Field23;
		[XmlElement("Nm1", typeof(int)),             XmlElement                                    ] public string? Field23a;

		#endregion

		#region Test Region 2

		public int     Field12;                                                         // Field3 comnt
		public string? Field22_____;
		public string? PField121    { get; set; }
		public string  PField122    { get { return "not null"; } }
		public string? PField221    { get { var a = 1; return null; } }
		public string? PField222    { get { return null; } }                            // Field3 comment
		public string? PField23     { get { return null; } set { value?.ToString(); } } // Fieomment

		#endregion

#if AAA

		/// <summary>
		/// 456
		/// </summary>
		[XmlArrayItem(typeof(int), DataType="List")]
		public List<int>? Field3; // Field3 comment

#endif

#if AAA

		[DisplayName("Prop"), XmlArrayItem(typeof(int), DataType="List")]
		public char Property1 // Property1 comment
		{
			get
			{
				int a = 1;
				return 'a';
			}
			set
			{
				var a = value;
				a.ToString();
			}
		}

#endif

		public char Property11
		{
			get { return 'a'; }
			set { var a = value; }
		}

		public List<int>? Field31;

		public double Field5;

		public List<int>? Field6;

		public double         Fld7;                               // Fld7
		public List<int>?     Field8;
		public DateTime       FieldLongName;                      // field long name
		public List<string?>? Property2     { get;         set; } // Property2
		public List<int?>?    Property3     { get; private set; } // Property3
		public int?           Prop1         { get;         set; } // Prop1

		public List<string?>? Field4;

		#region EditableObject support

		partial void BeforeAcceptChanges();
		partial void AfterAcceptChanges ();

		public virtual void AcceptChanges()
		{
			BeforeAcceptChanges();

			AcceptEditableString1Changes();
			AcceptEditableString2Changes();
			AcceptEditableLong1Changes();
			AcceptEditableInt1Changes();
			AcceptEditableInt3Changes();
			AcceptEditableDouble1Changes();

			AfterAcceptChanges();
		}

		partial void BeforeRejectChanges();
		partial void AfterRejectChanges ();

		public virtual void RejectChanges()
		{
			BeforeRejectChanges();

			RejectEditableString1Changes();
			RejectEditableString2Changes();
			RejectEditableLong1Changes();
			RejectEditableInt1Changes();
			RejectEditableInt3Changes();
			RejectEditableDouble1Changes();

			AfterRejectChanges();
		}

		public virtual bool IsDirty
		{
			get
			{
				return
					IsEditableString1Dirty ||
					IsEditableString2Dirty ||
					IsEditableLong1Dirty   ||
					IsEditableInt1Dirty    ||
					IsEditableInt3Dirty    ||
					IsEditableDouble1Dirty;
			}
		}

		#endregion

		#region IEditableObject support

		private bool _isEditing;
		public  bool  IsEditing { get { return _isEditing; } }

		public virtual void BeginEdit () { AcceptChanges(); _isEditing = true; }
		public virtual void CancelEdit() { _isEditing = false; RejectChanges(); }
		public virtual void EndEdit   () { _isEditing = false; AcceptChanges(); }

		#endregion

		#region INotifyPropertyChanged support

#if !SILVERLIGHT
		[field : NonSerialized]
#endif
		public virtual event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			var propertyChanged = PropertyChanged;

			if (propertyChanged != null)
			{
#if SILVERLIGHT
				if (System.Windows.Deployment.Current.Dispatcher.CheckAccess())
					propertyChanged(this, new PropertyChangedEventArgs(propertyName));
				else
					System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
						() =>
						{
							var pc = PropertyChanged;
							if (pc != null)
								pc(this, new PropertyChangedEventArgs(propertyName));
						});
#else
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
#endif
			}
		}

		protected void OnPropertyChanged(PropertyChangedEventArgs arg)
		{
			var propertyChanged = PropertyChanged;

			if (propertyChanged != null)
			{
#if SILVERLIGHT
				if (System.Windows.Deployment.Current.Dispatcher.CheckAccess())
					propertyChanged(this, arg);
				else
					System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
						() =>
						{
							var pc = PropertyChanged;
							if (pc != null)
								pc(this, arg);
						});
#else
				propertyChanged(this, arg);
#endif
			}
		}

		#endregion

		#region INotifyPropertyChanging support

#if !SILVERLIGHT
		[field : NonSerialized]
#endif
		public virtual event PropertyChangingEventHandler? PropertyChanging;

		protected void OnPropertyChanging(string propertyName)
		{
			var propertyChanging = PropertyChanging;

			if (propertyChanging != null)
			{
#if SILVERLIGHT
				if (System.Windows.Deployment.Current.Dispatcher.CheckAccess())
					propertyChanging(this, new PropertyChangingEventArgs(propertyName));
				else
					System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
						() =>
						{
							var pc = PropertyChanging;
							if (pc != null)
								pc(this, new PropertyChangingEventArgs(propertyName));
						});
#else
				propertyChanging(this, new PropertyChangingEventArgs(propertyName));
#endif
			}
		}

		protected void OnPropertyChanging(PropertyChangingEventArgs arg)
		{
			var propertyChanging = PropertyChanging;

			if (propertyChanging != null)
			{
#if SILVERLIGHT
				if (System.Windows.Deployment.Current.Dispatcher.CheckAccess())
					propertyChanging(this, arg);
				else
					System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
						() =>
						{
							var pc = PropertyChanging;
							if (pc != null)
								pc(this, arg);
						});
#else
				propertyChanging(this, arg);
#endif
			}
		}

		#endregion

		#region Validation

#if !SILVERLIGHT
		[field : NonSerialized]
#endif
		public int _isValidCounter;

		public static partial class CustomValidator
		{
			public static bool IsValid(TestClass1 obj)
			{
				try
				{
					obj._isValidCounter++;

					var flag0 = ValidationResult.Success == ValidateEditableString1(obj, obj.EditableString1);
					var flag1 = ValidationResult.Success == ValidateEditableString2(obj, obj.EditableString2);
					var flag2 = ValidationResult.Success == ValidateEditableLong1(obj, obj.EditableLong1);
					var flag3 = ValidationResult.Success == ValidateEditableInt1(obj, obj.EditableInt1);

					return flag0 || flag1 || flag2 || flag3;
				}
				finally
				{
					obj._isValidCounter--;
				}
			}

			public static ValidationResult ValidateEditableString1(TestClass1 obj, string value)
			{
				var list = new List<ValidationResult>();

				Validator.TryValidateProperty(
					value,
					new ValidationContext(obj, null, null) { MemberName = NameOfEditableString1 }, list);

				obj.ValidateEditableString1(value, list);

				if (list.Count > 0)
				{
					foreach (var result in list)
						foreach (var name in result.MemberNames)
							obj.AddError(name, result.ErrorMessage);

					return list[0];
				}

				obj.RemoveError(NameOfEditableString1);

				return ValidationResult.Success;
			}

			public static ValidationResult ValidateEditableString2(TestClass1 obj, string? value)
			{
				var list = new List<ValidationResult>();

				Validator.TryValidateProperty(
					value,
					new ValidationContext(obj, null, null) { MemberName = NameOfEditableString2 }, list);

				obj.ValidateEditableString2(value, list);

				if (list.Count > 0)
				{
					foreach (var result in list)
						foreach (var name in result.MemberNames)
							obj.AddError(name, result.ErrorMessage);

					return list[0];
				}

				obj.RemoveError(NameOfEditableString2);

				return ValidationResult.Success;
			}

			public static ValidationResult ValidateEditableLong1(TestClass1 obj, long value)
			{
				var list = new List<ValidationResult>();

				Validator.TryValidateProperty(
					value,
					new ValidationContext(obj, null, null) { MemberName = NameOfEditableLong1 }, list);

				obj.ValidateEditableLong1(value, list);

				if (list.Count > 0)
				{
					foreach (var result in list)
						foreach (var name in result.MemberNames)
							obj.AddError(name, result.ErrorMessage);

					return list[0];
				}

				obj.RemoveError(NameOfEditableLong1);

				return ValidationResult.Success;
			}

			public static ValidationResult ValidateEditableInt1(TestClass1 obj, int value)
			{
				var list = new List<ValidationResult>();

				Validator.TryValidateProperty(
					value,
					new ValidationContext(obj, null, null) { MemberName = NameOfEditableInt1 }, list);

				obj.ValidateEditableInt1(value, list);

				if (list.Count > 0)
				{
					foreach (var result in list)
						foreach (var name in result.MemberNames)
							obj.AddError(name, result.ErrorMessage);

					return list[0];
				}

				obj.RemoveError(NameOfEditableInt1);

				return ValidationResult.Success;
			}
		}

		partial void ValidateEditableString1(string value, List<ValidationResult> validationResults);
		partial void ValidateEditableString2(string? value, List<ValidationResult> validationResults);
		partial void ValidateEditableLong1  (long value, List<ValidationResult> validationResults);
		partial void ValidateEditableInt1   (int value, List<ValidationResult> validationResults);

		#endregion
	}

	[Serializable, DisplayName("TestClass")]
	public partial class TestClass2 : TestClass1
	{
	}

	public partial interface ITestInterface
	{
		#region Test Region

		int    P1 { get; set; }
		string P2 { get; set; }

		#endregion

		#region Test Region 2

		int PField121 { get; set; }

		string PField122 { get; set; }

		string PField221 { get; set; }

		#endregion

#if !AAA

		[DisplayName("Prop"), XmlArrayItem(typeof(int), DataType="List")]
		char Property1 { get; set; } // Property1 comment

#endif
	}
}

#nullable restore
#pragma warning restore 1591
