alter proc dbo.SetWorker
	@id int,
	@name varchar(255),
	@app varchar(255),
	@active bit,
	@host varchar(255),
	@settings varchar(2000),
	@executed datetime, 
	@error varchar(max) = '',
	@date datetime = null,
	@appFullName varchar(512) = null,
	@started datetime = null,
	@statusTimeout int = null
as 
begin 
	set nocount on;

	declare @hosts int = 0,
		@activeHosts int = 0;

	begin transaction; 

	select @hosts = count(1), @activeHosts = sum(iif(active = 1, 1, 0))
		from dbo.Workers 
		where name = @name 
			and app = @app
			and date >= dateadd(millisecond, -1 * isnull(@statusTimeout, 60), getdate())
			and @host != host;

	set @hosts = @hosts + 1;
	set @activeHosts = isnull(@activeHosts, 0);
	set @date = isnull(@date, getdate());

	update dbo.Workers 
		set date = @date, 
			active = @active,
			hosts = @hosts,
			error = @error,
			executed = @executed,
			appFullName = @appFullName,
			started = @started
		where (@id > 0 and id = @id) 
			or (host = @host 
				and name = @name 
				and app = @app); 
				
	if @@ROWCOUNT = 0 
		insert into dbo.Workers 
			(name, host, app, appFullName, date, settings, active, hosts, error, executed, started) 
		values 
			(@name, @host, @app, appFullName, @date, @settings, @active, @hosts, @error, @executed, @started); 
			
	select top 1 *, @activeHosts as activeHosts
		from dbo.Workers 
		where (@id > 0 and id = @id) 
			or (host = @host 
				and name = @name 
				and app = @app); 

	commit transaction;
end;
go