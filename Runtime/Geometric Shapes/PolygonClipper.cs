// by Freya Holmér (https://github.com/FreyaHolmer/Mathfs)

using System;
using System.Collections.Generic;
using UnityEngine;
using uPools;

namespace Freya {

	/// <summary>Utility type to clip polygons</summary>
	public static class PolygonClipper {

		enum PointSideState {
			Discard = -1,
			Edge = 0,
			Keep = 1,
			Handled = 2
		}

		public enum ResultState {
			OriginalLeftIntact,
			Clipped,
			FullyDiscarded
		}

		public class PolygonSection : IComparable<PolygonSection> {

			private static readonly ObjectPool<PolygonSection> pool = new ObjectPool<PolygonSection>( () => new PolygonSection( default, null ) );
			private static readonly List<PolygonSection> used = new List<PolygonSection>();

			public static PolygonSection Create(FloatRange tRange, List<Vector2> points)
			{
				var item = pool.Rent();
				used.Add(item);
				item.tRange = tRange;
				item.points = points;
				return item;
			}

			public static void RecyclePool()
			{
                foreach (var item in used)
                {
					pool.Return(item);
                }
				used.Clear();
            }

            public FloatRange tRange;
			public List<Vector2> points;
			public PolygonSection( FloatRange tRange, List<Vector2> points ) => ( this.tRange, this.points ) = ( tRange, points );
			public int CompareTo( PolygonSection other ) => tRange.Min.CompareTo( other.tRange.Min );
		}

        private static readonly ObjectPool<List<PolygonSection>> pool = new ObjectPool<List<PolygonSection>>(() => new List<PolygonSection>());
        private static readonly List<List<PolygonSection>> used = new List<List<PolygonSection>>();

        public static List<PolygonSection> CreateSortedSet()
        {
            var item = pool.Rent();
			item.Clear();
            used.Add(item);
            return item;
        }

        public static void RecyclePool()
        {
            foreach (var item in used)
            {
                pool.Return(item);
            }
            used.Clear();
        }

        static List<PointSideState> states = new List<PointSideState>();

		public static ResultState SplitConvex(Polygon poly, Line2D line, out List<Polygon> splitPolygons)
		{
            splitPolygons = ListPool<Polygon>.Create();
			Clip(poly, line, out var split1);
			Clip(poly, new Line2D(line.origin, -line.dir), out var split2);
			if (split1 != null)
			{
				splitPolygons.AddRange(split1);
			}
			if (split2 != null)
			{
				splitPolygons.AddRange(split2);
			}
            return ResultState.Clipped;
        }


        public static ResultState Clip( Polygon poly, Line2D line, out List<Polygon> clippedPolygons ) {
			states.Clear();

			// first, figure out which side all points are on
			bool hasDiscards = false;
			int startIndex = -1;
			int count = poly.Count;
			for( int i = 0; i < count; i++ ) {
				float sd = line.SignedDistance( poly[i] );
				if( Mathfs.Approximately( sd, 0 ) )
					states.Add( PointSideState.Edge );
				else if( sd > 0 ) {
					if( startIndex == -1 )
						startIndex = i;
					states.Add( PointSideState.Keep );
				} else {
					hasDiscards = true;
					states.Add( PointSideState.Discard );
				}
			}

			if( hasDiscards == false ) {
				clippedPolygons = null;
				return ResultState.OriginalLeftIntact;
			}

			if( startIndex == -1 ) {
				clippedPolygons = null;
				return ResultState.FullyDiscarded;
			}

			// find keep points, spread outwards until it's cut off from the rest
			var sections = CreateSortedSet();
			for( int i = 0; i < count; i++ ) {
				if( states[i] == PointSideState.Keep ) {
					sections.Add( ExtractPolygonSection( poly, line, i ) );
				}
			}
			sections.Sort();

            // combine all clipped polygonal regions
            clippedPolygons = ListPool<Polygon>.Create();

            while ( sections.Count > 0 ) {
				// find solid polygon
				PolygonSection solid = sections[0];
				sections.Remove( solid );
				int solidDir = solid.tRange.Direction;

				// find holes in that polygon
				float referencePoint = solid.tRange.Min;
				while( true ) { // should break early anyway
					FloatRange checkRange = new FloatRange( referencePoint, solid.tRange.Max );
                    var hole = FindSections(sections, solidDir, checkRange);
                    if ( hole == null ) {
						// nothing inside - we're done with this solid
						clippedPolygons.Add( Polygon.Create( solid.points ) );
						break;
					} else {
						// append the hole polygon to the solid points
						sections.Remove( hole );
						if( solidDir == 1 )
							solid.points.InsertRange( 0, hole.points );
						else
							solid.points.AddRange( hole.points );

						referencePoint = hole.tRange.Max; // skip everything inside the hole by shifting forward
					}
				}
			}

			return ResultState.Clipped;
		}

		static PolygonSection FindSections(List<PolygonSection> sections, int solidDir, FloatRange checkRange)
		{
			foreach(var s in sections)
			{
				if (s.tRange.Direction != solidDir && checkRange.Contains(s.tRange))
					return s;
			}
			return null;
		}


        static PolygonSection ExtractPolygonSection( Polygon poly, Line2D line, int sourceIndex ) {
			List<Vector2> points = ListPool<Vector2>.Create();

			void AddBack( int i ) {
				states[i.Mod( states.Count )] = PointSideState.Handled;
				points.Insert( 0, poly[i] );
			}

			void AddFront( int i ) {
				states[i.Mod( states.Count )] = PointSideState.Handled;
				points.Add( poly[i] );
			}
			
			AddFront( sourceIndex );

			float tStart = 0, tEnd = 0;
			for( int dir = -1; dir <= 1; dir += 2 ) {
				for( int i = 1; i < poly.Count; i++ ) {
					int index = sourceIndex + dir * i;
					var state = states[index.Mod(states.Count)];
                    if (state == PointSideState.Discard || state == PointSideState.Edge ) {
						// hit the front edge
						Line2D edge = new Line2D( poly[index - dir], poly[index] - poly[index - dir] );
						if( IntersectionTest.LinearTValues( line, edge, out float tLine, out float tEdge ) ) {
							Vector2 intPt = edge.GetPoint( tEdge );
							if( dir == 1 ) {
								tEnd = tLine;
								points.Add( intPt );
							} else {
								tStart = tLine;
								points.Insert( 0, intPt );
							}

							break;
						}

						throw new Exception( "Polygon clipping failed due to line intersection not working as expected. You may have duplicate points or a degenerate polygon in general" );
					}

					// haven't hit the end yet, add current point
					if( dir == 1 )
						AddFront( index );
					else
						AddBack( index );
				}
			}

			return PolygonSection.Create( ( tStart, tEnd ), points );
		}


	}

}