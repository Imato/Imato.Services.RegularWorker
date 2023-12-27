update workers 
	set executed = current_timestamp 
	where id = @id; 
select executed from workers where id = @id;