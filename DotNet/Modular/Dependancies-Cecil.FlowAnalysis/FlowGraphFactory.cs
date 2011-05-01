#region license
//
// (C) db4objects Inc. http://www.db4o.com
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using System;
using Cecil.FlowAnalysis.ActionFlow;
using Cecil.FlowAnalysis.ControlFlow;
using Mono.Cecil;

namespace Cecil.FlowAnalysis {
	/// <summary>
	/// Creates the specific graphs.
	/// </summary>
	public class FlowGraphFactory {

		public static ControlFlowGraph CreateControlFlowGraph (MethodDefinition method)
		{
			if (null == method) throw new ArgumentNullException ("method");
			ControlFlowGraphBuilder builder = new ControlFlowGraphBuilder (method);
			return builder.BuildGraph ();
		}

		public static ActionFlowGraph CreateActionFlowGraph (ControlFlowGraph cfg)
		{
			if (null == cfg) throw new ArgumentNullException ("cfg");
			ActionFlowGraphBuilder builder = new ActionFlowGraphBuilder (cfg);
			return builder.BuildGraph ();
		}

		public static ActionFlowGraph CreateActionFlowGraph (MethodDefinition method)
		{
			return CreateActionFlowGraph (CreateControlFlowGraph (method));
		}

		private FlowGraphFactory ()
		{
		}
	}
}
