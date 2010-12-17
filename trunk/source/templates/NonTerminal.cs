	// RULE-COMMENT
	private State METHOD-NAME(State _state, List<Result> _outResults)
	{
		State _start = _state;
		List<Result> results = new List<Result>();
		Console.WriteLine("RULE-NAME");			// {{debugging}}
		
		string fail = null;								// {{has-prolog and (prolog-uses-fail or pass-epilog-uses-fail)}}
		/* PROLOG-HOOK */							// {{has-prolog}}
		if (fail != null)									// {{has-prolog and prolog-uses-fail}}
		{													// {{has-prolog and prolog-uses-fail}}
			DoDebugFailure(_start.Index, "'DEBUG-NAME' failed prolog");				// {{has-prolog and prolog-uses-fail and (debug == 'failures' or debug == 'both')}}
			return new State(_start.Index, false, ErrorSet.Combine(_start.Errors, new ErrorSet(_start.Index, fail)));		// {{has-prolog and prolog-uses-fail}}
		}													// {{has-prolog and prolog-uses-fail}}
		
		/* RULE-BODY */
		
		if (_state.Parsed)								// {{has-pass-epilog}}
			/* PASS-EPILOG-HOOK */					// {{has-pass-epilog}}
		if (fail != null)									// {{has-pass-epilog and pass-epilog-uses-fail}}
			_state = new State(_start.Index, false, ErrorSet.Combine(_start.Errors, new ErrorSet(_state.Errors.Index, fail)));	// {{has-pass-epilog and pass-epilog-uses-fail}}
		
		if (_state.Parsed)
		{
			XmlElement _node = DoCreateElementNode("RULE-NAME", _start.Index, _state.Index - _start.Index, DoGetLine(_start.Index), DoGetCol(_start.Index), (from r in results where r.Value != null select r.Value).ToArray());	// {{value == 'XmlNode'}}
			_node.SetAttribute("alternative", "RULE-INDEX");							// {{value == 'XmlNode' and rule-has-alternatives}}
			VALUE value = _node;															// {{value == 'XmlNode'}}
			VALUE value = results.Count > 0 ? results[0].Value : default(VALUE);	// {{value != 'XmlNode' and value != 'void'}}
			
			string fatal = null;								// {{has-pass-action and pass-action-uses-fatal}}
			string text = m_input.Substring(_start.Index, _state.Index - _start.Index);	// {{has-pass-action and pass-action-uses-text}}
			
			/* PASS-ACTION */
			
			if (!string.IsNullOrEmpty(fatal))				// {{has-pass-action and pass-action-uses-fatal}}
				DoThrow(_start.Index, fatal);				// {{has-pass-action and pass-action-uses-fatal}}
			
			if (text != null)																										// {{has-pass-action and pass-action-uses-text}}
				_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));	// {{has-pass-action and pass-action-uses-text and value != 'void'}}
				_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input));			// {{has-pass-action and pass-action-uses-text and value == 'void'}}
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input, value));		// {{(not has-pass-action or not pass-action-uses-text) and value != 'void'}}
			_outResults.Add(new Result(this, _start.Index, _state.Index - _start.Index, m_input));				// {{(not has-pass-action or not pass-action-uses-text) and value == 'void'}}
		}
		else													// {{has-fail-action}}
		{														// {{has-fail-action}}
			string expected = null;						// {{has-fail-action and fail-action-uses-expected}}
			
			/* FAIL-ACTION */
			
			if (expected != null)							// {{has-fail-action and fail-action-uses-expected}}
				_state = new State(_start.Index, false, ErrorSet.Combine(_start.Errors, new ErrorSet(_state.Errors.Index, expected)));		// {{has-fail-action and fail-action-uses-expected}}
		}														// {{has-fail-action}}
		if (m_file == m_debugFile)																// {{debugging and has-debug-file}}
		{																								// {{debugging and has-debug-file}}
			if (_state.Parsed)																		// {{(debug == 'matches' or debug == 'both') and has-debug-file}}
				DoDebugMatch(_start.Index, _state.Index, "'DEBUG-NAME' parsed");		// {{(debug == 'matches' or debug == 'both') and has-debug-file}}
			if (!_state.Parsed)																		// {{(debug == 'failures' or debug == 'both') and has-debug-file}}
				DoDebugFailure(_start.Index, "'DEBUG-NAME' " + DoEscapeAll(_state.Errors.ToString()));	// {{(debug == 'failures' or debug == 'both') and has-debug-file}}
		}																								// {{debugging and has-debug-file}}
		if (_state.Parsed)																			// {{(debug == 'matches' or debug == 'both') and not has-debug-file}}
			DoDebugMatch(_start.Index, _state.Index, "'DEBUG-NAME' parsed");			// {{(debug == 'matches' or debug == 'both') and not has-debug-file}}
		if (!_state.Parsed)																			// {{(debug == 'failures' or debug == 'both') and not has-debug-file}}
			DoDebugFailure(_start.Index, "'DEBUG-NAME' " + DoEscapeAll(_state.Errors.ToString()));	// {{(debug == 'failures' or debug == 'both') and not has-debug-file}}
		
		/* EPILOG-HOOK */
		
		if (!_state.Parsed)									// {{has-fail-epilog}}
			/* FAIL-EPILOG-HOOK */						// {{has-fail-epilog}}

		return _state;
	}