using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace RBTB_WindowsClient_Frame.Helpers
{
	public static class EnumHelper
	{
		[CanBeNull]
		public static string GetEnumMemberAttribute<T>( this T enumValue ) where T : struct
		{
			var type = typeof( T );
			if ( !type.IsEnum )
				throw new ArgumentException( "Has to be of enum type!", nameof( enumValue ) );

			return GetFieldCustomAttribute<T, EnumMemberAttribute>( enumValue.ToString() )?.Value;
		}


		[CanBeNull]
		public static string GetEnumMemberAttributeValue<T>( T enumValue ) where T : struct
		{
			var type = typeof( T );
			if ( !type.IsEnum )
				throw new ArgumentException( "Has to be of enum type!", nameof( enumValue ) );

			return GetFieldCustomAttribute<T, EnumMemberAttribute>( enumValue.ToString() )?.Value;
		}

		[CanBeNull]
		public static TAttribute GetFieldCustomAttribute<TObject, TAttribute>( [NotNull] string name ) where TAttribute : class =>
			( typeof( TObject ).GetField( name )
			 ?? throw new ArgumentException( $"There is no such field as {name} in {typeof( TObject ).FullName}!", nameof( name ) ) )
			.GetCustomAttributes( false )
			.OfType<TAttribute>()
			.FirstOrDefault();

		///<summary>
		/// Расширение для всех enum проекта. Перевод из строки в enumType. Если не существует вернет DefaultEnumType
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="str"></param>
		/// <returns></returns>
		public static T ToEnum<T>( this string str )
		{
			var enumType = typeof( T );
			try
			{
				foreach ( var name in Enum.GetNames( enumType ) )
				{
					var enumMemberAttribute = ( (EnumMemberAttribute[])enumType
						.GetField( name )
						.GetCustomAttributes( typeof( EnumMemberAttribute ), true ) )
						.Single();
					if ( enumMemberAttribute.Value == str )
					{
						return (T)Enum.Parse( enumType, name );
					}
				}
				return default( T );
			}
			catch
			{
				return default( T );
			}
		}
		///<summary>
		/// Расширение для всех enum проекта. Перевод из строки в enumType. Если не существует вернет DefaultEnumType
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static IReadOnlyList<T> ToListEnum<T>( this IReadOnlyList<string> strList )
		{
			List<T> result = new List<T>();
			bool assept = false;
			foreach ( var str in strList )
			{
				var enumType = typeof( T );
				foreach ( var name in Enum.GetNames( enumType ) )
				{
					var enumMemberAttribute = ( (EnumMemberAttribute[])enumType
						.GetField( name )
						.GetCustomAttributes( typeof( EnumMemberAttribute ), true ) )
						.Single();
					if ( enumMemberAttribute.Value == str )
					{
						result.Add( (T)Enum.Parse( enumType, name ) );
						assept = true;
					}
				}
				if ( !assept )
					result.Add( default( T ) );
				assept = false;
			}
			return result;
		}
	}
}
