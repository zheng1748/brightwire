﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MathNet.Numerics.Distributions;

namespace BrightWire.Source.Helper
{
	/// <summary>
	/// 
	/// </summary>
    public class VectorDistanceHelper : IDisposable
	{
		readonly int _size;
		readonly ILinearAlgebraProvider _lap;
		readonly DistanceMetric _distanceMetric;
		readonly List<IVector> _comparison = new List<IVector>();
		readonly IReadOnlyList<IVector> _data;

	    public VectorDistanceHelper(ILinearAlgebraProvider lap, IReadOnlyList<IVector> data, DistanceMetric distanceMetric = DistanceMetric.Euclidean)
	    {
			_lap = lap;
		    _distanceMetric = distanceMetric;
		    _size = data[0].Count;
		    _data = data;
	    }

		public void Dispose()
		{
		}

		public IReadOnlyList<IVector> CompareTo => _comparison;

		public int AddComparison(IVector comparison)
		{
			var ret = _comparison.Count;
			_comparison.Add(comparison);
			return ret;
		}

		public void UpdateComparisonVector(int index, IVector newVector)
		{
			_comparison[index] = newVector;
		}

		public void SetComparisonVectors(IReadOnlyList<IVector> comparisonVectors)
		{
			_comparison.Clear();
			_comparison.AddRange(comparisonVectors);
		}

		(int Index, float Value) _GetMinimum(float[,] data, int columnIndex)
		{
			var len = _comparison.Count;
			if (len == 1)
				return (0, data[0, columnIndex]);

			var bestIndex = -1;
			var min = float.MaxValue;
			for (int j = 0; j < len; j++) {
				var val = data[j, columnIndex];
				if (val < min) {
					bestIndex = j;
					min = val;
				}
			}
			return (bestIndex, min);
		}

		public IReadOnlyList<int> GetClosest()
		{
			var distance = _lap.CalculateDistances(_data, _comparison, _distanceMetric);
			return Enumerable.Range(0, _data.Count)
				.Select(i => _GetMinimum(distance, i).Index)
				.ToList()
			;
		}

		//public IReadOnlyList<int> GetFurthest()
		//{
		//	var distance = _data.CalculateDistance(_comparison, _distanceMetric);
		//	return Enumerable.Range(0, distance.ColumnCount)
		//		.Select(i => distance.Column(i).MaximumIndex())
		//		.ToList()
		//	;
		//}

		public IDiscreteDistribution GetCategoricalDistribution()
		{
			var distance = _lap.CalculateDistances(_data, _comparison, _distanceMetric);
			var probability = Enumerable.Range(0, _data.Count)
				.Select(i => (double)_GetMinimum(distance, i).Value)
				.ToArray();
			return new Categorical(probability);
		}

		public IVector GetAverageFromData(IReadOnlyList<int> indices)
		{
			var data = _lap.CreateMatrixFromColumns(indices.Select(i => _data[i]).ToList());
			var result = data.RowSums();
			result.Multiply(1f / indices.Count);
			return result;
		}
	}
}
