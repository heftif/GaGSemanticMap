﻿using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;

namespace GaGSemanticMap.Skills
{
	public interface ICheckInputFunction
	{
		/// <summary>
		/// Checks validity of the input
		/// </summary>
		public Task<string> ValidateInputAsync(string message, SKContext context);
	}
}
