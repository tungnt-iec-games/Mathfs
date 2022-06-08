// by Freya Holmér (https://github.com/FreyaHolmer/Mathfs)
// Do not manually edit - this file is generated by MathfsCodegen.cs

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Freya {

	/// <summary>An optimized uniform 1D Cubic bézier segment, with 4 control points</summary>
	[Serializable] public struct BezierCubic1D : IParamCubicSplineSegment1D {

		const MethodImplOptions INLINE = MethodImplOptions.AggressiveInlining;

		/// <summary>Creates a uniform 1D Cubic bézier segment, from 4 control points</summary>
		/// <param name="p0">The starting point of the curve</param>
		/// <param name="p1">The second control point of the curve, sometimes called the start tangent point</param>
		/// <param name="p2">The third control point of the curve, sometimes called the end tangent point</param>
		/// <param name="p3">The end point of the curve</param>
		public BezierCubic1D( float p0, float p1, float p2, float p3 ) {
			( this.p0, this.p1, this.p2, this.p3 ) = ( p0, p1, p2, p3 );
			validCoefficients = false;
			curve = default;
		}

		Polynomial curve;
		public Polynomial Curve {
			get {
				ReadyCoefficients();
				return curve;
			}
		}
		#region Control Points

		[SerializeField] float p0, p1, p2, p3;
		public Matrix4x1 PointMatrix => new(p0, p1, p2, p3);

		/// <summary>The starting point of the curve</summary>
		public float P0 {
			[MethodImpl( INLINE )] get => p0;
			[MethodImpl( INLINE )] set => _ = ( p0 = value, validCoefficients = false );
		}

		/// <summary>The second control point of the curve, sometimes called the start tangent point</summary>
		public float P1 {
			[MethodImpl( INLINE )] get => p1;
			[MethodImpl( INLINE )] set => _ = ( p1 = value, validCoefficients = false );
		}

		/// <summary>The third control point of the curve, sometimes called the end tangent point</summary>
		public float P2 {
			[MethodImpl( INLINE )] get => p2;
			[MethodImpl( INLINE )] set => _ = ( p2 = value, validCoefficients = false );
		}

		/// <summary>The end point of the curve</summary>
		public float P3 {
			[MethodImpl( INLINE )] get => p3;
			[MethodImpl( INLINE )] set => _ = ( p3 = value, validCoefficients = false );
		}

		/// <summary>Get or set a control point position by index. Valid indices from 0 to 3</summary>
		public float this[ int i ] {
			get =>
				i switch {
					0 => P0,
					1 => P1,
					2 => P2,
					3 => P3,
					_ => throw new ArgumentOutOfRangeException( nameof(i), $"Index has to be in the 0 to 3 range, and I think {i} is outside that range you know" )
				};
			set {
				switch( i ) {
					case 0:
						P0 = value;
						break;
					case 1:
						P1 = value;
						break;
					case 2:
						P2 = value;
						break;
					case 3:
						P3 = value;
						break;
					default: throw new ArgumentOutOfRangeException( nameof(i), $"Index has to be in the 0 to 3 range, and I think {i} is outside that range you know" );
				}
			}
		}

		#endregion
		[NonSerialized] bool validCoefficients;

		[MethodImpl( INLINE )] void ReadyCoefficients() {
			if( validCoefficients )
				return; // no need to update
			validCoefficients = true;
			curve = new Polynomial( CharMatrix.cubicBezier * PointMatrix );
		}
		public static bool operator ==( BezierCubic1D a, BezierCubic1D b ) => a.P0 == b.P0 && a.P1 == b.P1 && a.P2 == b.P2 && a.P3 == b.P3;
		public static bool operator !=( BezierCubic1D a, BezierCubic1D b ) => !( a == b );
		public bool Equals( BezierCubic1D other ) => P0.Equals( other.P0 ) && P1.Equals( other.P1 ) && P2.Equals( other.P2 ) && P3.Equals( other.P3 );
		public override bool Equals( object obj ) => obj is BezierCubic1D other && Equals( other );
		public override int GetHashCode() => HashCode.Combine( p0, p1, p2, p3 );

		public override string ToString() => $"({p0}, {p1}, {p2}, {p3})";
		public static explicit operator HermiteCubic1D( BezierCubic1D s ) =>
			new HermiteCubic1D(
				s.p0,
				-3*s.p0+3*s.p1,
				s.p3,
				-3*s.p2+3*s.p3
			);
		public static explicit operator CatRomCubic1D( BezierCubic1D s ) =>
			new CatRomCubic1D(
				6*s.p0-6*s.p1+s.p3,
				s.p0,
				s.p3,
				s.p0-6*s.p2+6*s.p3
			);
		public static explicit operator UBSCubic1D( BezierCubic1D s ) =>
			new UBSCubic1D(
				6*s.p0-7*s.p1+2*s.p2,
				2*s.p1-s.p2,
				-s.p1+2*s.p2,
				2*s.p1-7*s.p2+6*s.p3
			);
		/// <summary>Returns a linear blend between two bézier curves</summary>
		/// <param name="a">The first spline segment</param>
		/// <param name="b">The second spline segment</param>
		/// <param name="t">A value from 0 to 1 to blend between <c>a</c> and <c>b</c></param>
		public static BezierCubic1D Lerp( BezierCubic1D a, BezierCubic1D b, float t ) =>
			new(
				Mathfs.Lerp( a.p0, b.p0, t ),
				Mathfs.Lerp( a.p1, b.p1, t ),
				Mathfs.Lerp( a.p2, b.p2, t ),
				Mathfs.Lerp( a.p3, b.p3, t )
			);
		/// <summary>Splits this curve at the given t-value, into two curves that together form the exact same shape</summary>
		/// <param name="t">The t-value to split at</param>
		public (BezierCubic1D pre, BezierCubic1D post) Split( float t ) {
			float a = p0 + ( p1 - p0 ) * t;
			float b = p1 + ( p2 - p1 ) * t;
			float c = p2 + ( p3 - p2 ) * t;
			float d = a + ( b - a ) * t;
			float e = b + ( c - b ) * t;
			float p = d + ( e - d ) * t;
			return ( new BezierCubic1D( p0, a, d, p ), new BezierCubic1D( p, e, c, p3 ) );
		}
	}
}
