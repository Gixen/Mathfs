﻿// by Freya Holmér (https://github.com/FreyaHolmer/Mathfs)
// a lot of stuff here made possible by this excellent writeup on bezier curves: https://pomax.github.io/bezierinfo/

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Freya.Mathfs;

namespace Freya {

	// Bezier math
	// A lot of the following code is unrolled into floats and components for performance reasons.
	// It's much faster than keeping the more readable function calls and vector types unfortunately

	/// <summary>An optimized 3D cubic bezier curve, with 4 control points</summary>
	[Serializable] public struct BezierCubic3D {

		const MethodImplOptions INLINE = MethodImplOptions.AggressiveInlining;
		
		/// <inheritdoc cref="BezierCubic2D(Vector2,Vector2,Vector2,Vector2)"/>
		public BezierCubic3D( Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3 ) {
			( this.p0, this.p1, this.p2, this.p3 ) = ( p0, p1, p2, p3 );
			validCoefficients = false;
			c3 = c2 = c1 = default;
		}

		#region Control Points

		[SerializeField] Vector3 p0, p1, p2, p3; // the points of the curve

		/// <inheritdoc cref="BezierCubic2D.P0"/>
		public Vector3 P0 {
			[MethodImpl( INLINE )] get => p0;
			[MethodImpl( INLINE )] set => _ = ( p0 = value, validCoefficients = false );
		}

		/// <inheritdoc cref="BezierCubic2D.P1"/>
		public Vector3 P1 {
			[MethodImpl( INLINE )] get => p1;
			[MethodImpl( INLINE )] set => _ = ( p1 = value, validCoefficients = false );
		}

		/// <inheritdoc cref="BezierCubic2D.P2"/>
		public Vector3 P2 {
			[MethodImpl( INLINE )] get => p2;
			[MethodImpl( INLINE )] set => _ = ( p2 = value, validCoefficients = false );
		}

		/// <inheritdoc cref="BezierCubic2D.P3"/>
		public Vector3 P3 {
			[MethodImpl( INLINE )] get => p3;
			[MethodImpl( INLINE )] set => _ = ( p3 = value, validCoefficients = false );
		}

		/// <inheritdoc cref="BezierCubic2D.this"/> 
		public Vector3 this[ int i ] {
			get {
				switch( i ) {
					case 0:  return P0;
					case 1:  return P1;
					case 2:  return P2;
					case 3:  return P3;
					default: throw new IndexOutOfRangeException();
				}
			}
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
					default: throw new IndexOutOfRangeException();
				}
			}
		}

		#endregion

		#region Coefficients

		[NonSerialized] bool validCoefficients; // inverted isDirty flag (can't default to true in structs)
		[NonSerialized] Vector3 c3, c2, c1; // cached coefficients for fast evaluation. c0 = p0

		// Coefficient Calculation
		[MethodImpl( INLINE )] void ReadyCoefficients() {
			if( validCoefficients )
				return; // no need to update
			validCoefficients = true;
			c3.x = 3 * ( p1.x - p2.x ) + ( p3.x - p0.x );
			c2.x = 3 * ( p0.x - p1.x + p2.x - p1.x );
			c1.x = 3 * ( p1.x - p0.x );
			c3.y = 3 * ( p1.y - p2.y ) + ( p3.y - p0.y );
			c2.y = 3 * ( p0.y - p1.y + p2.y - p1.y );
			c1.y = 3 * ( p1.y - p0.y );
			c3.z = 3 * ( p1.z - p2.z ) + ( p3.z - p0.z );
			c2.z = 3 * ( p0.z - p1.z + p2.z - p1.z );
			c1.z = 3 * ( p1.z - p0.z );
		}

		/// <inheritdoc cref="BezierCubic2D.C0"/>
		public Vector3 C0 {
			[MethodImpl( INLINE )] get => p0;
		}

		/// <inheritdoc cref="BezierCubic2D.C1"/>
		public Vector3 C1 {
			[MethodImpl( INLINE )] get {
				ReadyCoefficients();
				return c1;
			}
		}

		/// <inheritdoc cref="BezierCubic2D.C2"/>
		public Vector3 C2 {
			[MethodImpl( INLINE )] get {
				ReadyCoefficients();
				return c2;
			}
		}

		/// <inheritdoc cref="BezierCubic2D.C3"/>
		public Vector3 C3 {
			[MethodImpl( INLINE )] get {
				ReadyCoefficients();
				return c3;
			}
		}

		/// <inheritdoc cref="BezierCubic2D.GetCoefficients()"/>
		[MethodImpl( INLINE )] public (Vector3 c3, Vector3 c2, Vector3 c1, Vector3 c0) GetCoefficients() {
			ReadyCoefficients();
			return ( c3, c2, c1, p0 );
		}

		#endregion

		// Object comparison stuff

		#region Object Comparison & ToString

		public static bool operator ==( BezierCubic3D a, BezierCubic3D b ) => a.P0 == b.P0 && a.P1 == b.P1 && a.P2 == b.P2 && a.P3 == b.P3;
		public static bool operator !=( BezierCubic3D a, BezierCubic3D b ) => !( a == b );
		public bool Equals( BezierCubic3D other ) => P0.Equals( other.P0 ) && P1.Equals( other.P1 ) && P2.Equals( other.P2 ) && P3.Equals( other.P3 );
		public override bool Equals( object obj ) => obj is BezierCubic3D other && Equals( other );

		public override int GetHashCode() {
			unchecked {
				int hashCode = P0.GetHashCode();
				hashCode = ( hashCode * 397 ) ^ P1.GetHashCode();
				hashCode = ( hashCode * 397 ) ^ P2.GetHashCode();
				hashCode = ( hashCode * 397 ) ^ P3.GetHashCode();
				return hashCode;
			}
		}

		public override string ToString() => $"{P0}, {P1}, {P2}, {P3}";

		#endregion

		#region Type Casting

		/// <summary>Returns this bezier curve flattened to the Z plane, effectively setting z to 0</summary>
		/// <param name="bezierCubic3D">The 3D curve to cast and flatten on the Z plane</param>
		public static explicit operator BezierCubic2D( BezierCubic3D bezierCubic3D ) {
			return new BezierCubic2D( bezierCubic3D.P0, bezierCubic3D.P1, bezierCubic3D.P2, bezierCubic3D.P3 );
		}

		#endregion

		// Base properties - Points, Derivatives & Tangents

		#region Points & Derivatives

		/// <inheritdoc cref="BezierCubic2D.GetPoint(float)"/>
		[MethodImpl( INLINE )] public Vector3 GetPoint( float t ) {
			ReadyCoefficients();
			float t2 = t * t;
			float t3 = t2 * t;
			return new Vector3( t3 * c3.x + t2 * c2.x + t * c1.x + p0.x, t3 * c3.y + t2 * c2.y + t * c1.y + p0.y, t3 * c3.z + t2 * c2.z + t * c1.z + p0.z );
		}

		/// <inheritdoc cref="BezierCubic2D.GetDerivative(float)"/>
		[MethodImpl( INLINE )] public Vector3 GetDerivative( float t ) {
			ReadyCoefficients();
			float t2 = t * t;
			return new Vector3( 3 * t2 * c3.x + 2 * t * c2.x + c1.x, 3 * t2 * c3.y + 2 * t * c2.y + c1.y, 3 * t2 * c3.z + 2 * t * c2.z + c1.z );
		}

		/// <inheritdoc cref="BezierCubic2D.GetSecondDerivative(float)"/>
		[MethodImpl( INLINE )] public Vector3 GetSecondDerivative( float t ) {
			ReadyCoefficients();
			return new Vector3( 6 * t * c3.x + 2 * c2.x, 6 * t * c3.y + 2 * c2.y, 6 * t * c3.z + 2 * c2.z );
		}

		/// <inheritdoc cref="BezierCubic2D.GetThirdDerivative()"/>
		[MethodImpl( INLINE )] public Vector3 GetThirdDerivative() {
			ReadyCoefficients();
			return new Vector3( 6 * c3.x, 6 * c3.y, 6 * c3.z );
		}

		#endregion

		#region Point Components

		/// <summary>Returns the X coordinate at the given t-value on the curve</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		[MethodImpl( INLINE )] public float GetPointX( float t ) {
			ReadyCoefficients();
			float t2 = t * t;
			float t3 = t2 * t;
			return t3 * c3.x + t2 * c2.x + t * c1.x + p0.x;
		}

		/// <summary>Returns the Y coordinate at the given t-value on the curve</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		[MethodImpl( INLINE )] public float GetPointY( float t ) {
			ReadyCoefficients();
			float t2 = t * t;
			float t3 = t2 * t;
			return t3 * c3.y + t2 * c2.y + t * c1.y + p0.y;
		}

		/// <summary>Returns the Z coordinate at the given t-value on the curve</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		[MethodImpl( INLINE )] public float GetPointZ( float t ) {
			ReadyCoefficients();
			float t2 = t * t;
			float t3 = t2 * t;
			return t3 * c3.z + t2 * c2.z + t * c1.z + p0.z;
		}

		/// <summary>Returns a component of the coordinate at the given t-value on the curve</summary>
		/// <param name="component">Which component of the coordinate to return. 0 is X, 1 is Y, 2 is Z</param>
		/// <param name="t">The t-value along the curve to sample</param>
		public float GetPointComponent( int component, float t ) {
			switch( component ) {
				case 0:  return GetPointX( t );
				case 1:  return GetPointY( t );
				case 2:  return GetPointZ( t );
				default: throw new ArgumentOutOfRangeException( nameof(component), "component has to be either 0, 1 or 2" );
			}
		}

		#endregion

		#region Tangent

		/// <summary>Returns the normalized tangent direction at the given t-value on the curve</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		public Vector3 GetTangent( float t ) => GetDerivative( t ).normalized;

		#endregion

		// Curvature & Torsion

		#region Curvature

		/// <summary>Returns a pseudovector at the given t-value on the curve, where the magnitude is the curvature in radians per distance unit, and the direction is the axis of curvature</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		public Vector3 GetCurvature( float t ) {
			( Vector3 vel, Vector3 acc ) = GetFirstTwoDerivatives( t );
			float dMag = vel.magnitude;
			return Vector3.Cross( vel, acc ) / ( dMag * dMag * dMag );
		}

		#endregion

		#region Torsion

		/// <summary>Returns the torsion at the given t-value on the curve, in radians per distance unit</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		public float GetTorsion( float t ) {
			( Vector3 vel, Vector3 acc, Vector3 jerk ) = GetAllThreeDerivatives( t );
			Vector3 cVector = Vector3.Cross( vel, acc );
			return Vector3.Dot( cVector, jerk ) / cVector.sqrMagnitude;
		}

		#endregion

		#region Osculating Circle

		/// <summary>Returns the osculating circle at the given t-value in the curve, if possible. Osculating circles are defined everywhere except on inflection points, where curvature is 0</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		public Circle3D GetOsculatingCircle( float t ) {
			( Vector3 point, Vector3 vel, Vector3 acc ) = GetPointAndFirstTwoDerivatives( t );
			float dMag = vel.magnitude;
			Vector3 curvatureVector = Vector3.Cross( vel, acc ) / ( dMag * dMag * dMag );
			( Vector3 axis, float curvature ) = curvatureVector.GetDirAndMagnitude();
			Vector3 normal = Vector3.Cross( vel, Vector3.Cross( acc, vel ) ).normalized;
			float signedRadius = 1f / curvature;
			return new Circle3D( point + normal * signedRadius, axis, Abs( signedRadius ) );
		}

		#endregion

		// Normals, Orientation & Angles

		#region Normal

		/// <summary>Returns the frenet-serret (curvature-based) normal direction at the given t-value on the curve</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		public Vector3 GetArcNormal( float t ) {
			( Vector3 vel, Vector3 acc ) = GetFirstTwoDerivatives( t );
			return Vector3.Cross( vel, Vector3.Cross( acc, vel ) ).normalized;
		}

		/// <summary>Returns a normal of the curve given a reference up vector and t-value on the curve.
		/// The normal will be perpendicular to both the supplied up vector and the curve</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		/// <param name="up">The reference up vector. The normal will be perpendicular to both the supplied up vector and the curve</param>
		public Vector3 GetNormal( float t, Vector3 up ) {
			Vector3 vel = GetDerivative( t );
			return Vector3.Cross( up, vel ).normalized;
		}

		#endregion

		#region Binormal

		/// <summary>Returns the frenet-serret (curvature-based) binormal direction at the given t-value on the curve</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		public Vector3 GetArcBinormal( float t ) {
			( Vector3 vel, Vector3 acc ) = GetFirstTwoDerivatives( t );
			return Vector3.Cross( vel, acc ).normalized;
		}

		/// <summary>Returns the binormal of the curve given a reference up vector and t-value on the curve.
		/// The binormal will attempt to be as aligned with the reference vector as possible,
		/// while still being perpendicular to the curve</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		/// <param name="up">The reference up vector. The binormal will attempt to be as aligned with the reference vector as possible, while still being perpendicular to the curve</param>
		public Vector3 GetBinormal( float t, Vector3 up ) {
			Vector3 vel = GetDerivative( t );
			Vector3 normal = Vector3.Cross( up, vel ).normalized;
			return Vector3.Cross( vel.normalized, normal );
		}

		#endregion

		#region Orientation

		/// <summary>Returns the orientation at the given point t, where the Z direction is tangent to the curve.
		/// The Y axis will attempt to align with the supplied up vector</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		/// <param name="up">The reference up vector. The Y axis will attempt to be as aligned with this vector as much as possible</param>
		public Quaternion GetOrientation( float t, Vector3 up ) => Quaternion.LookRotation( GetDerivative( t ), up );

		/// <summary>Returns the frenet-serret (curvature-based) orientation of curve at the given point t, where the Z direction is tangent to the curve.
		/// The X axis will point to the inner arc of the current curvature</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		public Quaternion GetArcOrientation( float t ) {
			( Vector3 vel, Vector3 acc ) = GetFirstTwoDerivatives( t );
			Vector3 binormal = Vector3.Cross( vel, acc );
			return Quaternion.LookRotation( vel, binormal );
		}

		#endregion

		#region Pose

		/// <summary>Returns the position and orientation of curve at the given point t, where the Z direction is tangent to the curve.
		/// The Y axis will attempt to align with the supplied up vector</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		/// <param name="up">The reference up vector. The Y axis will attempt to be as aligned with this vector as much as possible</param>
		public Pose GetPose( float t, Vector3 up ) {
			( Vector2 p, Vector2 v ) = GetPointAndTangent( t );
			return new Pose( p, Quaternion.LookRotation( v, up ) );
		}

		/// <summary>Returns the position and the frenet-serret (curvature-based) orientation of curve at the given point t, where the Z direction is tangent to the curve.
		/// The X axis will point to the inner arc of the current curvature</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		public Pose GetArcPose( float t ) {
			( Vector3 pt, Vector3 vel, Vector3 acc ) = GetPointAndFirstTwoDerivatives( t );
			Vector3 binormal = Vector3.Cross( vel, acc );
			return new Pose( pt, Quaternion.LookRotation( vel, binormal ) );
		}

		#endregion

		#region Matrix

		/// <summary>Returns the position and orientation of curve at the given point t, expressed as a matrix, where the Z direction is tangent to the curve.
		/// The Y axis will attempt to align with the supplied up vector</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		/// <param name="up">The reference up vector. The Y axis will attempt to be as aligned with this vector as much as possible</param>
		public Matrix4x4 GetMatrix( float t, Vector3 up ) {
			( Vector3 P, Vector3 T ) = GetPointAndTangent( t );
			Vector3 N = Vector3.Cross( up, T ).normalized; // X axis
			Vector3 B = Vector3.Cross( T, N ); // Y axis
			return new Matrix4x4(
				new Vector4( N.x, N.y, N.z, 0 ),
				new Vector4( B.x, B.y, B.z, 0 ),
				new Vector4( T.x, T.y, T.z, 0 ),
				new Vector4( P.x, P.y, P.z, 1 )
			);
		}

		/// <summary>Returns the position and the frenet-serret (curvature-based) orientation of curve at the given point t, expressed as a matrix, where the Z direction is tangent to the curve.
		/// The X axis will point to the inner arc of the current curvature</summary>
		/// <param name="t">The t-value along the curve to sample</param>
		public Matrix4x4 GetArcMatrix( float t ) {
			( Vector3 P, Vector3 vel, Vector3 acc ) = GetPointAndFirstTwoDerivatives( t );
			Vector3 T = vel.normalized;
			Vector3 B = Vector3.Cross( vel, acc ).normalized;
			Vector3 N = Vector3.Cross( B, T );
			return new Matrix4x4(
				new Vector4( N.x, N.y, N.z, 0 ),
				new Vector4( B.x, B.y, B.z, 0 ),
				new Vector4( T.x, T.y, T.z, 0 ),
				new Vector4( P.x, P.y, P.z, 1 )
			);
		}

		#endregion

		// Whole-curve properties & functions

		#region Splitting

		/// <inheritdoc cref="BezierCubic2D.Split(float)"/>
		public (BezierCubic3D pre, BezierCubic3D post) Split( float t ) {
			Vector3 a = new Vector3(
				P0.x + ( P1.x - P0.x ) * t,
				P0.y + ( P1.y - P0.y ) * t,
				P0.z + ( P1.z - P0.z ) * t );
			float bx = P1.x + ( P2.x - P1.x ) * t;
			float by = P1.y + ( P2.y - P1.y ) * t;
			float bz = P1.z + ( P2.z - P1.z ) * t;
			Vector3 c = new Vector3(
				P2.x + ( P3.x - P2.x ) * t,
				P2.y + ( P3.y - P2.y ) * t,
				P2.z + ( P3.z - P2.z ) * t );
			Vector3 d = new Vector3(
				a.x + ( bx - a.x ) * t,
				a.y + ( by - a.y ) * t,
				a.z + ( bz - a.z ) * t );
			Vector3 e = new Vector3(
				bx + ( c.x - bx ) * t,
				by + ( c.y - by ) * t,
				bz + ( c.z - bz ) * t );
			Vector3 p = new Vector3(
				d.x + ( e.x - d.x ) * t,
				d.y + ( e.y - d.y ) * t,
				d.z + ( e.z - d.z ) * t );
			return ( new BezierCubic3D( P0, a, d, p ), new BezierCubic3D( p, e, c, P3 ) );
		}

		#endregion

		#region Bounds

		/// <inheritdoc cref="BezierCubic2D.GetBounds()"/>
		public Bounds GetBounds() {
			// first and last points are always included
			Vector3 min = Vector3.Min( P0, P3 );
			Vector3 max = Vector3.Max( P0, P3 );

			void Encapsulate( int axis, float value ) {
				min[axis] = Min( min[axis], value );
				max[axis] = Max( max[axis], value );
			}

			for( int i = 0; i < 3; i++ ) {
				ResultsMax2<float> extrema = GetLocalExtremaPoints( i );
				for( int j = 0; j < extrema.count; j++ )
					Encapsulate( i, extrema[j] );
			}

			return new Bounds( ( max + min ) * 0.5f, max - min );
		}

		#endregion

		#region Length

		/// <inheritdoc cref="BezierCubic2D.GetLength(int)"/>
		public float GetLength( int accuracy = 8 ) {
			if( accuracy <= 2 )
				return ( P0 - P3 ).magnitude;

			float totalDist = 0;
			Vector3 prev = P0;
			for( int i = 1; i < accuracy; i++ ) {
				float t = i / ( accuracy - 1f );
				Vector3 p = GetPoint( t );
				float dx = p.x - prev.x;
				float dy = p.y - prev.y;
				float dz = p.z - prev.z;
				totalDist += Mathf.Sqrt( dx * dx + dy * dy + dz * dz );
				prev = p;
			}

			return totalDist;
		}

		#endregion

		#region Project Point

		/// <inheritdoc cref="BezierCubic2D.ProjectPoint(Vector2,int,int)"/>
		public Vector3 ProjectPoint( Vector3 point, int initialSubdivisions = 16, int refinementIterations = 4 ) => ProjectPoint( point, out _, initialSubdivisions, refinementIterations );

		struct PointProjectSample {
			public float t;
			public float distDeltaSq;
			public Vector3 f;
			public Vector3 fp;
		}

		static PointProjectSample[] pointProjectGuesses = { default, default, default };

		/// <inheritdoc cref="BezierCubic2D.ProjectPoint(Vector2,out float,int,int)"/>
		public Vector3 ProjectPoint( Vector3 point, out float t, int initialSubdivisions = 16, int refinementIterations = 4 ) {
			// define a bezier relative to the test point
			BezierCubic3D bez = new BezierCubic3D( P0 - point, P1 - point, P2 - point, P3 - point );

			PointProjectSample SampleDistSqDelta( float tSmp ) {
				PointProjectSample s = new PointProjectSample { t = tSmp };
				( s.f, s.fp ) = bez.GetPointAndDerivative( tSmp );
				s.distDeltaSq = Vector3.Dot( s.f, s.fp );
				return s;
			}

			// find initial candidates
			int candidatesFound = 0;
			PointProjectSample prevSmp = SampleDistSqDelta( 0 );

			for( int i = 1; i < initialSubdivisions; i++ ) {
				float ti = i / ( initialSubdivisions - 1f );
				PointProjectSample smp = SampleDistSqDelta( ti );
				if( SignAsInt( smp.distDeltaSq ) != SignAsInt( prevSmp.distDeltaSq ) ) {
					pointProjectGuesses[candidatesFound++] = SampleDistSqDelta( ( prevSmp.t + smp.t ) / 2 );
					if( candidatesFound == 3 ) break; // no more than three possible candidates because of the polynomial degree
				}

				prevSmp = smp;
			}

			// refine each guess w. Newton-Raphson iterations
			void Refine( ref PointProjectSample smp ) {
				Vector3 fpp = bez.GetSecondDerivative( smp.t );
				float tNew = smp.t - Vector3.Dot( smp.f, smp.fp ) / ( Vector3.Dot( smp.f, fpp ) + Vector3.Dot( smp.fp, smp.fp ) );
				smp = SampleDistSqDelta( tNew );
			}

			for( int p = 0; p < candidatesFound; p++ )
				for( int i = 0; i < refinementIterations; i++ )
					Refine( ref pointProjectGuesses[p] );

			// Now find closest. First include the endpoints
			float sqDist0 = bez.P0.sqrMagnitude; // include endpoints
			float sqDist1 = bez.P3.sqrMagnitude;
			bool firstClosest = sqDist0 < sqDist1;
			float tClosest = firstClosest ? 0 : 1;
			Vector3 ptClosest = firstClosest ? P0 : P3;
			float distSqClosest = firstClosest ? sqDist0 : sqDist1;

			// then check internal roots
			for( int i = 0; i < candidatesFound; i++ ) {
				float pSqmag = pointProjectGuesses[i].f.sqrMagnitude;
				if( pSqmag < distSqClosest ) {
					distSqClosest = pSqmag;
					tClosest = pointProjectGuesses[i].t;
					ptClosest = pointProjectGuesses[i].f + point;
				}
			}

			t = tClosest;
			return ptClosest;
		}

		#endregion

		// Multi-eval fast paths

		#region Point & Derivative combos

		/// <inheritdoc cref="BezierCubic2D.GetPointAndTangent(float)"/> 
		public (Vector3, Vector3) GetPointAndTangent( float t ) {
			( Vector3 p, Vector3 d ) = GetPointAndDerivative( t );
			return ( p, d.normalized );
		}

		/// <inheritdoc cref="BezierCubic2D.GetPointAndDerivative(float)"/> 
		public (Vector3, Vector3) GetPointAndDerivative( float t ) {
			ReadyCoefficients();
			float t2 = t * t;
			float tx2 = t * 2;
			float t2x3 = t2 * 3;
			float t3 = t2 * t;
			return (
				new Vector3(
					t3 * c3.x + t2 * c2.x + t * c1.x + p0.x,
					t3 * c3.y + t2 * c2.y + t * c1.y + p0.y,
					t3 * c3.z + t2 * c2.z + t * c1.z + p0.z
				),
				new Vector3(
					t2x3 * c3.x + tx2 * c2.x + c1.x,
					t2x3 * c3.y + tx2 * c2.y + c1.y,
					t2x3 * c3.z + tx2 * c2.z + c1.z
				)
			);
		}

		/// <inheritdoc cref="BezierCubic2D.GetFirstTwoDerivatives(float)"/> 
		public (Vector3, Vector3) GetFirstTwoDerivatives( float t ) {
			ReadyCoefficients();
			float t2x3 = 3 * t * t;
			float tx2 = 2 * t;
			float tx6 = 6 * t;
			return (
				new Vector3( t2x3 * c3.x + tx2 * c2.x + c1.x, t2x3 * c3.y + tx2 * c2.y + c1.y, t2x3 * c3.z + tx2 * c2.z + c1.z ),
				new Vector3( tx6 * c3.x + 2 * c2.x, tx6 * c3.y + 2 * c2.y, tx6 * c3.z + 2 * c2.z )
			);
		}

		/// <inheritdoc cref="BezierCubic2D.GetAllThreeDerivatives(float)"/>
		public (Vector3, Vector3, Vector3) GetAllThreeDerivatives( float t ) {
			ReadyCoefficients();
			float t2x3 = 3 * t * t;
			float tx2 = 2 * t;
			float tx6 = 6 * t;
			return (
				new Vector3( t2x3 * c3.x + tx2 * c2.x + c1.x, t2x3 * c3.y + tx2 * c2.y + c1.y, t2x3 * c3.z + tx2 * c2.z + c1.z ),
				new Vector3( tx6 * c3.x + 2 * c2.x, tx6 * c3.y + 2 * c2.y, tx6 * c3.z + 2 * c2.z ),
				new Vector3( 6 * c3.x, 6 * c3.y, 6 * c3.z )
			);
		}

		/// <inheritdoc cref="BezierCubic2D.GetPointAndFirstTwoDerivatives(float)"/>
		public (Vector3, Vector3, Vector3) GetPointAndFirstTwoDerivatives( float t ) {
			ReadyCoefficients();
			float t2 = t * t;
			float tx2 = t * 2;
			float tx6 = t * 6;
			float t2x3 = t2 * 3;
			float t3 = t2 * t;
			return (
				new Vector3(
					t3 * c3.x + t2 * c2.x + t * c1.x + p0.x,
					t3 * c3.y + t2 * c2.y + t * c1.y + p0.y,
					t3 * c3.z + t2 * c2.z + t * c1.z + p0.z
				),
				new Vector3(
					t2x3 * c3.x + tx2 * c2.x + c1.x,
					t2x3 * c3.y + tx2 * c2.y + c1.y,
					t2x3 * c3.z + tx2 * c2.z + c1.z
				),
				new Vector3(
					tx6 * c3.x + 2 * c2.x,
					tx6 * c3.y + 2 * c2.y,
					tx6 * c3.z + 2 * c2.z
				)
			);
		}

		#endregion

		// Esoteric math stuff

		#region Polynomial Factors

		/// <summary>Returns the factors of the derivative polynomials, per-component, in the form at²+bt+c</summary>
		public (Vector3 a, Vector3 b, Vector3 c) GetDerivativeFactors() {
			Polynomial X = SplineUtils.GetCubicPolynomialDerivative( P0.x, P1.x, P2.x, P3.x );
			Polynomial Y = SplineUtils.GetCubicPolynomialDerivative( P0.y, P1.y, P2.y, P3.y );
			Polynomial Z = SplineUtils.GetCubicPolynomialDerivative( P0.z, P1.z, P2.z, P3.z );
			return (
				new Vector3( X.fQuadratic, Y.fQuadratic, Z.fQuadratic ),
				new Vector3( X.fLinear, Y.fLinear, Z.fLinear ),
				new Vector3( X.fConstant, Y.fConstant, Z.fConstant ) );
		}

		/// <summary>Returns the factors of the second derivative polynomials, per-component, in the form at+b</summary>
		public (Vector3 a, Vector3 b) GetSecondDerivativeFactors() {
			Polynomial X = SplineUtils.GetCubicPolynomialSecondDerivative( P0.x, P1.x, P2.x, P3.x );
			Polynomial Y = SplineUtils.GetCubicPolynomialSecondDerivative( P0.y, P1.y, P2.y, P3.y );
			Polynomial Z = SplineUtils.GetCubicPolynomialSecondDerivative( P0.z, P1.z, P2.z, P3.z );
			return (
				new Vector3( X.fLinear, Y.fLinear, Z.fLinear ),
				new Vector3( X.fConstant, Y.fConstant, Z.fConstant ) );
		}

		#endregion

		#region Local Extrema

		/// <summary>Returns the t values of extrema (local minima/maxima) on a given axis in the 0 &lt; t &lt; 1 range</summary>
		/// <param name="axis">Either 0 (X), 1 (Y) or 2 (Z)</param>
		public ResultsMax2<float> GetLocalExtrema( int axis ) {
			if( axis < 0 || axis > 2 )
				throw new ArgumentOutOfRangeException( nameof(axis), "axis has to be either 0, 1 or 2" );
			Polynomial polynom;
			if( axis == 0 ) // a little silly but the vec[] indexers are kinda expensive
				polynom = SplineUtils.GetCubicPolynomialDerivative( P0.x, P1.x, P2.x, P3.x );
			else if( axis == 1 )
				polynom = SplineUtils.GetCubicPolynomialDerivative( P0.y, P1.y, P2.y, P3.y );
			else
				polynom = SplineUtils.GetCubicPolynomialDerivative( P0.z, P1.z, P2.z, P3.z );
			ResultsMax3<float> roots = polynom.Roots;
			ResultsMax2<float> outPts = default;
			for( int i = 0; i < roots.count; i++ ) {
				float t = roots[i];
				if( t.Between( 0, 1 ) )
					outPts = outPts.Add( t );
			}

			return outPts;
		}

		/// <summary>Returns the extrema points (local minima/maxima points) on a given axis in the 0 &lt; t &lt; 1 range</summary>
		/// <param name="axis">Either 0 (X), 1 (Y) or 2 (Z)</param>
		public ResultsMax2<float> GetLocalExtremaPoints( int axis ) {
			ResultsMax2<float> t = GetLocalExtrema( axis );
			ResultsMax2<float> pts = default;
			for( int i = 0; i < t.count; i++ )
				pts = pts.Add( GetPointComponent( axis, t[i] ) );
			return pts;
		}

		#endregion

	}

}